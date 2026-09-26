using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    public class UniversalExportService : IUniversalExportService
    {
        private readonly string _connectionString;
        private readonly ILogger<UniversalExportService> _logger;

        public UniversalExportService(IConfiguration configuration, ILogger<UniversalExportService> logger)
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

            _logger.LogInformation("Starting export for report: {ReportName} in format {Format} using SP: {StoredProcedure} (@status='{Status}')",
                config.ReportName, format, config.StoredProcedure, config.Status);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(config.StoredProcedure, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 300; // 5 minute safety timeout for massive datasets

            // Standard SP base inputs
            command.Parameters.AddWithValue("@status", config.Status);
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
                AddOutputParam(command, "@QTY", SqlDbType.Int);
                AddOutputParam(command, "@HU_VALIDATED_QTY", SqlDbType.Int);
                AddOutputParam(command, "@HU_WRONG_QTY", SqlDbType.Int);
                AddOutputParam(command, "@HHT_VALIDATE_QTY", SqlDbType.Int);
                AddOutputParam(command, "@ENCODED_QTY", SqlDbType.Int);
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
                AddOutputParam(command, "@STORECOUNT", SqlDbType.Int);
                AddOutputParam(command, "@WHCOUNT", SqlDbType.Int);
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
                // Default: Professional XLSX Workbook
                hasData = await ExportXlsxAsync(reader, outputStream, config.ReportName, cancellationToken);
            }

            if (hasData)
            {
                _logger.LogInformation("Export completed successfully for {ReportName} in {Format}", config.ReportName, format);
            }
            return hasData;
        }

        private async Task<bool> ExportXlsxAsync(SqlDataReader reader, Stream outputStream, string reportName, CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook();
            string sheetTitle = reportName.Length > 28 ? reportName.Substring(0, 28) : reportName;
            var worksheet = workbook.Worksheets.Add(sheetTitle);

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

            // Styled Header
            for (int c = 0; c < validColIndices.Count; c++)
            {
                int colIndex = validColIndices[c];
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = reader.GetName(colIndex);
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromArgb(15, 23, 42); // slate-900
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(241, 245, 249); // slate-100
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorderColor = XLColor.FromArgb(203, 213, 225);
            }

            int rowIdx = 2;
            while (await reader.ReadAsync(cancellationToken))
            {
                for (int c = 0; c < validColIndices.Count; c++)
                {
                    int colIndex = validColIndices[c];
                    if (!reader.IsDBNull(colIndex))
                    {
                        object val = reader.GetValue(colIndex);
                        var cell = worksheet.Cell(rowIdx, c + 1);

                        if (val is int or long or short or byte)
                        {
                            cell.Value = Convert.ToInt64(val);
                        }
                        else if (val is decimal dec)
                        {
                            cell.Value = dec;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (val is double or float)
                        {
                            cell.Value = Convert.ToDouble(val);
                            cell.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (val is DateTime dt)
                        {
                            cell.Value = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            cell.Value = val.ToString() ?? "";
                        }
                    }
                }
                rowIdx++;
                if (rowIdx > 1_048_570)
                {
                    _logger.LogWarning("Reached maximum Excel worksheet row capacity (1,048,570 rows) for {ReportName}", reportName);
                    break;
                }
            }

            if (rowIdx == 2)
            {
                return false;
            }

            if (validColIndices.Count > 0)
            {
                worksheet.SheetView.FreezeRows(1);
                if (rowIdx <= 10_000)
                {
                    worksheet.Columns(1, validColIndices.Count).AdjustToContents(10.0, 50.0);
                }
                else
                {
                    worksheet.Columns(1, validColIndices.Count).Width = 18;
                }
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;
            await ms.CopyToAsync(outputStream, cancellationToken);
            return true;
        }

        private async Task<bool> ExportCsvAsync(SqlDataReader reader, Stream outputStream, CancellationToken cancellationToken)
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

            long rowCount = 0;
            var rowBuffer = new string[validColIndices.Count];

            while (await reader.ReadAsync(cancellationToken))
            {
                for (int c = 0; c < validColIndices.Count; c++)
                {
                    int colIndex = validColIndices[c];
                    if (reader.IsDBNull(colIndex))
                    {
                        rowBuffer[c] = "\"\"";
                    }
                    else
                    {
                        object val = reader.GetValue(colIndex);
                        string strVal = val switch
                        {
                            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                            DateOnly d => d.ToString("yyyy-MM-dd"),
                            _ => val.ToString() ?? ""
                        };
                        rowBuffer[c] = EscapeCsv(strVal);
                    }
                }

                await writer.WriteLineAsync(string.Join(",", rowBuffer));
                rowCount++;

                if (rowCount % 1000 == 0)
                {
                    await writer.FlushAsync();
                }
            }

            await writer.FlushAsync();
            return rowCount > 0;
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
