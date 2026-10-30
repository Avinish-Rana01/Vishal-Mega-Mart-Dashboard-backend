using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    public class UniversalExportService : IUniversalExportService
    {
        private readonly string _connectionString;
        private readonly ILogger<UniversalExportService> _logger;

        public UniversalExportService(Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<UniversalExportService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        public async Task<bool> StreamExportAsync(UniversalExportRequest request, Stream outputStream, CancellationToken cancellationToken)
        {
            var config = ReportRegistry.GetConfig(request.ReportName);
            if (config == null)
            {
                throw new ArgumentException($"Unknown reportName: '{request.ReportName}'");
            }

            string format = string.IsNullOrWhiteSpace(request.Format) ? "xlsx" : request.Format.Trim().ToLowerInvariant();

            // Resolve SP status dynamically (supports single-date vs date-range branching)
            string resolvedStatus = config.ResolveStatus(request);

            _logger.LogInformation("Starting streaming export for report: {ReportName} in format {Format} using SP: {StoredProcedure} (@status='{Status}')",
                config.ReportName, format, config.StoredProcedure, resolvedStatus);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(config.StoredProcedure, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 300; // 5 minute safety timeout for massive datasets

            // Standard SP base inputs
            command.Parameters.AddWithValue("@status", resolvedStatus);
            command.Parameters.AddWithValue("@PageIndex", 1);
            command.Parameters.AddWithValue("@PageSize", 10_000_000); // 10 Million (safe from integer overflow in SQL Server)

            string sortCol = !string.IsNullOrWhiteSpace(request.SortColumn) ? request.SortColumn.Trim() : config.DefaultSortColumn;
            string sortDir = !string.IsNullOrWhiteSpace(request.SortDirection) ? request.SortDirection.Trim() : config.DefaultSortDirection;
            command.Parameters.AddWithValue("@SortColumn", sortCol);
            command.Parameters.AddWithValue("@SortDirection", sortDir);

            // Execute specific parameter binder
            config.ParameterBinder(command, request);

            // Bind User_ID for role-based store scoping & permissions
            if (!command.Parameters.Contains("@User_ID"))
            {
                int userId = request.UserId ?? 0;
                if (userId == 0 && !string.IsNullOrWhiteSpace(request.User) && int.TryParse(request.User, out var parsedUid))
                {
                    userId = parsedUid;
                }
                command.Parameters.AddWithValue("@User_ID", userId);
            }

            // Add procedure-specific OUTPUT parameters
            if (string.Equals(config.StoredProcedure, "SP_NEW_DASHBOARD", StringComparison.OrdinalIgnoreCase))
            {
                AddOutputParam(command, "@RecordCount", SqlDbType.Int);
                AddOutputParam(command, "@TotalCount", SqlDbType.Int);
                AddOutputParam(command, "@PROCESSED_HU", SqlDbType.Int);
                AddOutputParam(command, "@UNPROCESSED_HU", SqlDbType.Int);
                AddOutputParam(command, "@PROCESSED_ARTICLE_QTY", SqlDbType.Int);
                AddOutputParam(command, "@QTY", SqlDbType.Int);
                AddOutputParam(command, "@HU_VALIDATED_QTY", SqlDbType.Int);
                AddOutputParam(command, "@HU_WRONG_QTY", SqlDbType.Int);
                AddOutputParam(command, "@HHT_VALIDATE_QTY", SqlDbType.Int);
                AddOutputParam(command, "@ENCODED_QTY", SqlDbType.Int);
                AddOutputParam(command, "@DIFF_QTY", SqlDbType.Int);
                AddOutputParam(command, "@DPOS_SALE", SqlDbType.Int);
                AddOutputParam(command, "@RFID_CHECKOUT", SqlDbType.Int);
                AddOutputParam(command, "@TAFFETA_SALE", SqlDbType.Int);
                AddOutputParam(command, "@MANUAL_SALE", SqlDbType.Int);
            }
            else // SP_NEW_REPORT
            {
                AddOutputParam(command, "@RecordCount", SqlDbType.Int);
                AddOutputParam(command, "@TotalCount", SqlDbType.Int);
                AddOutputParam(command, "@QTY", SqlDbType.Int);
                AddOutputParam(command, "@ACTUALQTY", SqlDbType.Int);
                AddOutputParam(command, "@SCANQTY", SqlDbType.Int);
                AddOutputParam(command, "@DIFFQTY", SqlDbType.Int);
                AddOutputParam(command, "@Excess_Qty", SqlDbType.Int);
                AddOutputParam(command, "@HUCOUNT", SqlDbType.Int);
                AddOutputParam(command, "@MATERIALCOUNT", SqlDbType.Int);
                AddOutputParam(command, "@TAGQTY", SqlDbType.Int);
                AddOutputParam(command, "@STORECOUNT", SqlDbType.Int);
                AddOutputParam(command, "@WHCOUNT", SqlDbType.Int);
                AddOutputParam(command, "@TAG_VALIDATED_QTY", SqlDbType.Int);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Advance past any non-result-set statements (e.g. SELECT INTO #TEMP tables where FieldCount == 0)
            while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken))
            {
            }

            if (!reader.HasRows)
            {
                _logger.LogInformation("No rows found for export request {ReportName}", config.ReportName);
                return false;
            }

            bool hasData;
            if (format == "csv")
            {
                hasData = await ExportCsvAsync(reader, outputStream, cancellationToken);
            }
            else
            {
                // Ultra-Fast Streaming XLSX via MiniExcel (SAX Writer with FastMode)
                hasData = await ExportXlsxAsync(reader, outputStream, config.ReportName, cancellationToken);
            }

            if (hasData)
            {
                _logger.LogInformation("Export streaming completed successfully for {ReportName} in {Format}", config.ReportName, format);
            }
            return hasData;
        }

        private static async Task<bool> ExportXlsxAsync(
            SqlDataReader reader,
            Stream outputStream,
            string reportName,
            CancellationToken cancellationToken)
        {
            var validColIndices = new List<int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                if (string.Equals(colName, "RowNumber", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                validColIndices.Add(i);
            }

            string sheetTitle = reportName.Length > 28 ? reportName.Substring(0, 28) : reportName;

            // Stream rows directly into MiniExcel using lazy iterator with pre-cached column names
            var rows = EnumerateReaderRows(reader, validColIndices);

            await MiniExcel.SaveAsAsync(
                outputStream,
                rows,
                printHeader: true,
                sheetName: sheetTitle,
                excelType: ExcelType.XLSX,
                cancellationToken: cancellationToken);

            return true;
        }

        private static IEnumerable<Dictionary<string, object?>> EnumerateReaderRows(
            SqlDataReader reader,
            List<int> validColIndices)
        {
            int colCount = validColIndices.Count;
            var colNames = new string[colCount];
            var colIndices = validColIndices.ToArray();

            // Pre-cache column names once to save millions of repetitive string allocations in the loop
            for (int c = 0; c < colCount; c++)
            {
                colNames[c] = reader.GetName(colIndices[c]);
            }

            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(colCount);
                for (int c = 0; c < colCount; c++)
                {
                    int colIndex = colIndices[c];
                    string colName = colNames[c];

                    if (reader.IsDBNull(colIndex))
                    {
                        row[colName] = null;
                    }
                    else
                    {
                        object val = reader.GetValue(colIndex);
                        if (val is int or long or short or byte)
                        {
                            row[colName] = Convert.ToInt64(val);
                        }
                        else if (val is decimal dec)
                        {
                            row[colName] = dec;
                        }
                        else if (val is double or float)
                        {
                            row[colName] = Convert.ToDouble(val);
                        }
                        else if (val is DateTime dt)
                        {
                            row[colName] = dt.TimeOfDay == TimeSpan.Zero
                                ? dt.ToString("yyyy-MM-dd")
                                : dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else if (val is DateOnly d)
                        {
                            row[colName] = d.ToString("yyyy-MM-dd");
                        }
                        else if (val is DateTimeOffset dto)
                        {
                            row[colName] = dto.TimeOfDay == TimeSpan.Zero
                                ? dto.ToString("yyyy-MM-dd")
                                : dto.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            string strVal = val.ToString() ?? "";
                            if (strVal.EndsWith(" 00:00:00"))
                            {
                                strVal = strVal.Substring(0, strVal.Length - 9);
                            }
                            else if (strVal.EndsWith("T00:00:00"))
                            {
                                strVal = strVal.Substring(0, strVal.Length - 9);
                            }
                            row[colName] = strVal;
                        }
                    }
                }
                yield return row;
            }
        }

        private async Task<bool> ExportCsvAsync(
            SqlDataReader reader,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            if (reader.FieldCount == 0)
            {
                return false;
            }

            await using var writer = new StreamWriter(outputStream, new UTF8Encoding(true), bufferSize: 65536, leaveOpen: true);

            var validColIndices = new List<int>();
            var headers = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                if (string.Equals(colName, "RowNumber", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                validColIndices.Add(i);
                headers.Add(EscapeCsv(colName));
            }

            await writer.WriteLineAsync(string.Join(",", headers));

            long rowCount = await WriteReaderRowsToCsvAsync(reader, writer, validColIndices, cancellationToken);

            await writer.FlushAsync(cancellationToken);
            return rowCount > 0;
        }

        private static async Task<long> WriteReaderRowsToCsvAsync(
            SqlDataReader reader,
            StreamWriter writer,
            List<int> validColIndices,
            CancellationToken cancellationToken)
        {
            long count = 0;
            int colCount = validColIndices.Count;
            var colIndices = validColIndices.ToArray();
            var rowBuffer = new string[colCount];

            while (await reader.ReadAsync(cancellationToken))
            {
                for (int c = 0; c < colCount; c++)
                {
                    int colIndex = colIndices[c];
                    if (reader.IsDBNull(colIndex))
                    {
                        rowBuffer[c] = "\"\"";
                    }
                    else
                    {
                        object val = reader.GetValue(colIndex);
                        string strVal = val switch
                        {
                            DateTime dt => dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm:ss"),
                            DateOnly d => d.ToString("yyyy-MM-dd"),
                            DateTimeOffset dto => dto.TimeOfDay == TimeSpan.Zero ? dto.ToString("yyyy-MM-dd") : dto.ToString("yyyy-MM-dd HH:mm:ss"),
                            _ => val.ToString() ?? ""
                        };

                        if (strVal.EndsWith(" 00:00:00"))
                        {
                            strVal = strVal.Substring(0, strVal.Length - 9);
                        }
                        else if (strVal.EndsWith("T00:00:00"))
                        {
                            strVal = strVal.Substring(0, strVal.Length - 9);
                        }

                        rowBuffer[c] = EscapeCsv(strVal);
                    }
                }

                await writer.WriteLineAsync(string.Join(",", rowBuffer));
                count++;

                if (count % 1000 == 0)
                {
                    await writer.FlushAsync(cancellationToken);
                }
            }
            return count;
        }

        private static void AddOutputParam(SqlCommand cmd, string paramName, SqlDbType type)
        {
            if (!cmd.Parameters.Contains(paramName))
            {
                var p = new SqlParameter(paramName, type)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(p);
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
