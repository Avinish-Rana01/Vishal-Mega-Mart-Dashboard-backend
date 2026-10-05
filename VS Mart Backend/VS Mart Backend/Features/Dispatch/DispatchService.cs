using ClosedXML.Excel;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dispatch
{
    public class DispatchService : IDispatchService
    {
        private readonly string _connectionString;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(IConfiguration configuration, ILogger<DispatchService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        private static string NormalizeColumn(string col)
        {
            if (string.IsNullOrWhiteSpace(col)) return string.Empty;
            return new string(col.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        public async Task<DispatchUploadResponse> ProcessUploadAsync(IFormFile file, string status, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "Please select a valid Excel file."
                };
            }

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "Only .xlsx and .xls files are supported."
                };
            }

            string normalizedStatus = (status ?? string.Empty).Trim().ToUpperInvariant();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null || worksheet.LastRowUsed() == null)
                {
                    return new DispatchUploadResponse
                    {
                        Success = false,
                        Message = "The uploaded Excel file contains no data or worksheets."
                    };
                }

                if (normalizedStatus == "RDC_MASTER")
                {
                    return await ProcessRdcMasterUploadAsync(worksheet, cancellationToken);
                }
                else if (normalizedStatus == "HU_INPUT")
                {
                    return await ProcessHuInputUploadAsync(worksheet, cancellationToken);
                }
                else if (normalizedStatus == "PICKLIST")
                {
                    var picklistResult = await ProcessPicklistUploadInternalAsync(worksheet, cancellationToken);
                    return new DispatchUploadResponse
                    {
                        Success = picklistResult.Success,
                        Message = picklistResult.Message,
                        Status = "PICKLIST",
                        TotalRows = picklistResult.TotalRecords,
                        InsertedRows = picklistResult.TotalRecords
                    };
                }
                else
                {
                    return new DispatchUploadResponse
                    {
                        Success = false,
                        Message = $"Unknown upload status '{status}'. Supported values: RDC_MASTER, HU_INPUT, PICKLIST."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dispatch upload for status {Status}", status);
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = $"Error processing file: {ex.Message}"
                };
            }
        }

        private async Task<DispatchUploadResponse> ProcessRdcMasterUploadAsync(IXLWorksheet worksheet, CancellationToken cancellationToken)
        {
            var headerRow = worksheet.Row(1);
            var colMap = new Dictionary<string, int>();

            int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (int col = 1; col <= lastCol; col++)
            {
                string header = headerRow.Cell(col).GetString();
                string norm = NormalizeColumn(header);
                if (!string.IsNullOrEmpty(norm) && !colMap.ContainsKey(norm))
                {
                    colMap[norm] = col;
                }
            }

            // Map aliases
            int getCol(params string[] aliases)
            {
                foreach (var a in aliases)
                {
                    string norm = NormalizeColumn(a);
                    if (colMap.TryGetValue(norm, out int colIdx)) return colIdx;
                }
                return 0;
            }

            int zoneCol = getCol("ZONE");
            int stateCol = getCol("RDCSTATE", "STATE", "RDC_STATE");
            int shedCol = getCol("SHED", "RDCSHADD", "RDCSHED", "RDC_SH_ADD", "VEHICLETYPE");
            int siteCol = getCol("SITE", "RECEIVINGSITE", "RECEIVING_SITE");
            int storeDescCol = getCol("STOREDESCRIPTION", "STOREDESC", "STORE_DESCRIPTION", "SDESC");
            int rdcCodeCol = getCol("RDCCODE", "RDC", "RDC_CODE", "DAY");

            if (zoneCol == 0 || siteCol == 0)
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "Missing required column: ZONE or SITE (or RECEIVING SITE)"
                };
            }

            var rdcDt = new DataTable();
            rdcDt.Columns.Add("ZONE", typeof(string));
            rdcDt.Columns.Add("RDC_STATE", typeof(string));
            rdcDt.Columns.Add("SHED", typeof(string));
            rdcDt.Columns.Add("SITE", typeof(string));
            rdcDt.Columns.Add("STORE_DESCRIPTION", typeof(string));
            rdcDt.Columns.Add("RDC_CODE", typeof(string));

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int totalRows = 0;

            for (int row = 2; row <= lastRow; row++)
            {
                var r = worksheet.Row(row);
                string zone = zoneCol > 0 ? r.Cell(zoneCol).GetString().Trim() : string.Empty;
                string site = siteCol > 0 ? r.Cell(siteCol).GetString().Trim() : string.Empty;
                string rdcCode = rdcCodeCol > 0 ? r.Cell(rdcCodeCol).GetString().Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(zone) && string.IsNullOrWhiteSpace(site) && string.IsNullOrWhiteSpace(rdcCode))
                    continue;

                totalRows++;

                DataRow dr = rdcDt.NewRow();
                dr["ZONE"] = zone;
                dr["RDC_STATE"] = stateCol > 0 ? r.Cell(stateCol).GetString().Trim() : string.Empty;
                dr["SHED"] = shedCol > 0 ? r.Cell(shedCol).GetString().Trim() : string.Empty;
                dr["SITE"] = site;
                dr["STORE_DESCRIPTION"] = storeDescCol > 0 ? r.Cell(storeDescCol).GetString().Trim() : string.Empty;
                dr["RDC_CODE"] = rdcCode;

                rdcDt.Rows.Add(dr);
            }

            if (rdcDt.Rows.Count == 0)
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "The Excel file contains no valid data rows."
                };
            }

            // Execute SP_INSERT_DISPATCH_MST
            var duplicates = new List<Dictionary<string, object>>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand("SP_INSERT_DISPATCH_MST", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ACTION", "INSERT_RDC_MASTER");

            var dataParam = cmd.Parameters.AddWithValue("@Data", rdcDt);
            dataParam.SqlDbType = SqlDbType.Structured;
            dataParam.TypeName = "dbo.RDC_Master_Type";

            var emptyHuDt = CreateEmptyHuTable();
            var data2Param = cmd.Parameters.AddWithValue("@Data2", emptyHuDt);
            data2Param.SqlDbType = SqlDbType.Structured;
            data2Param.TypeName = "dbo.HU_Master_Type";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                }
                duplicates.Add(dict);
            }

            int dupCount = duplicates.Count;
            int inserted = totalRows - dupCount;

            return new DispatchUploadResponse
            {
                Success = true,
                Status = "RDC_MASTER",
                Message = $"Processed {totalRows} rows. {inserted} new records inserted, {dupCount} duplicate records skipped.",
                TotalRows = totalRows,
                InsertedRows = inserted > 0 ? inserted : 0,
                DuplicateRows = dupCount,
                Duplicates = duplicates
            };
        }

        private async Task<DispatchUploadResponse> ProcessHuInputUploadAsync(IXLWorksheet worksheet, CancellationToken cancellationToken)
        {
            var headerRow = worksheet.Row(1);
            var colMap = new Dictionary<string, int>();

            int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (int col = 1; col <= lastCol; col++)
            {
                string header = headerRow.Cell(col).GetString();
                string norm = NormalizeColumn(header);
                if (!string.IsNullOrEmpty(norm) && !colMap.ContainsKey(norm))
                {
                    colMap[norm] = col;
                }
            }

            int getCol(params string[] aliases)
            {
                foreach (var a in aliases)
                {
                    string norm = NormalizeColumn(a);
                    if (colMap.TryGetValue(norm, out int colIdx)) return colIdx;
                }
                return 0;
            }

            int huCol = getCol("HUNO", "HUNUMBER", "HU_NO", "HU_NUMBER");
            int storeCol = getCol("STORE", "STORECODE", "STORE_CODE");
            int storeDescCol = getCol("STOREDESC", "STOREDESCRIPTION", "STORE_DESC");
            int whDescCol = getCol("WHDESC", "WSDESC", "WSDESCRIPTION", "WHDESCRIPTION", "WH_DESC");
            int createdDateCol = getCol("CREATEDDATE", "DATE", "CREATED_DATE");
            int transferValCol = getCol("TRANSFERVALUE", "TRANSFERVAL", "TRANSFER_VALUE");
            int countCol = getCol("COUNT", "QTY");

            if (huCol == 0)
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "Missing required column: HU NO / HU NUMBER"
                };
            }

            var huDt = CreateEmptyHuTable();
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int totalRows = 0;

            for (int row = 2; row <= lastRow; row++)
            {
                var r = worksheet.Row(row);
                string huNo = huCol > 0 ? r.Cell(huCol).GetString().Trim() : string.Empty;
                string store = storeCol > 0 ? r.Cell(storeCol).GetString().Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(huNo) && string.IsNullOrWhiteSpace(store))
                    continue;

                totalRows++;

                DataRow dr = huDt.NewRow();
                dr["HU_No"] = huNo;
                dr["Store"] = store;
                dr["Store_Desc"] = storeDescCol > 0 ? r.Cell(storeDescCol).GetString().Trim() : string.Empty;
                dr["WH_DESC"] = whDescCol > 0 ? r.Cell(whDescCol).GetString().Trim() : string.Empty;

                string rawDate = createdDateCol > 0 ? r.Cell(createdDateCol).GetString().Trim() : string.Empty;
                dr["Created_Date"] = string.IsNullOrWhiteSpace(rawDate) ? DateTime.Now.ToString("yyyy-MM-dd") : rawDate;

                decimal transferVal = 0;
                if (transferValCol > 0)
                {
                    decimal.TryParse(r.Cell(transferValCol).GetString(), out transferVal);
                }
                dr["Transfer_Value"] = transferVal;

                int countVal = 1;
                if (countCol > 0)
                {
                    int.TryParse(r.Cell(countCol).GetString(), out countVal);
                }
                dr["COUNT"] = countVal;

                huDt.Rows.Add(dr);
            }

            if (huDt.Rows.Count == 0)
            {
                return new DispatchUploadResponse
                {
                    Success = false,
                    Message = "The Excel file contains no valid HU records."
                };
            }

            var duplicates = new List<Dictionary<string, object>>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand("SP_INSERT_DISPATCH_MST", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ACTION", "INSERT_HU_MASTER");

            var emptyRdcDt = CreateEmptyRdcTable();
            var dataParam = cmd.Parameters.AddWithValue("@Data", emptyRdcDt);
            dataParam.SqlDbType = SqlDbType.Structured;
            dataParam.TypeName = "dbo.RDC_Master_Type";

            var data2Param = cmd.Parameters.AddWithValue("@Data2", huDt);
            data2Param.SqlDbType = SqlDbType.Structured;
            data2Param.TypeName = "dbo.HU_Master_Type";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                }
                duplicates.Add(dict);
            }

            int dupCount = duplicates.Count;
            int inserted = totalRows - dupCount;

            return new DispatchUploadResponse
            {
                Success = true,
                Status = "HU_INPUT",
                Message = $"Processed {totalRows} rows. {inserted} new HU records inserted, {dupCount} duplicate records skipped.",
                TotalRows = totalRows,
                InsertedRows = inserted > 0 ? inserted : 0,
                DuplicateRows = dupCount,
                Duplicates = duplicates
            };
        }

        private static DataTable CreateEmptyRdcTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ZONE", typeof(string));
            dt.Columns.Add("RDC_STATE", typeof(string));
            dt.Columns.Add("SHED", typeof(string));
            dt.Columns.Add("SITE", typeof(string));
            dt.Columns.Add("STORE_DESCRIPTION", typeof(string));
            dt.Columns.Add("RDC_CODE", typeof(string));
            return dt;
        }

        private static DataTable CreateEmptyHuTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("HU_No", typeof(string));
            dt.Columns.Add("Store", typeof(string));
            dt.Columns.Add("Store_Desc", typeof(string));
            dt.Columns.Add("WH_DESC", typeof(string));
            dt.Columns.Add("Created_Date", typeof(string));
            dt.Columns.Add("Transfer_Value", typeof(decimal));
            dt.Columns.Add("COUNT", typeof(int));
            return dt;
        }

        public async Task<PicklistUploadResponse> ProcessPicklistUploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return new PicklistUploadResponse
                {
                    Success = false,
                    Message = "Please select a valid Excel file."
                };
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null || worksheet.LastRowUsed() == null)
                {
                    return new PicklistUploadResponse
                    {
                        Success = false,
                        Message = "The Excel file contains no worksheets or data."
                    };
                }

                return await ProcessPicklistUploadInternalAsync(worksheet, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing picklist Excel upload");
                return new PicklistUploadResponse
                {
                    Success = false,
                    Message = $"Error processing file: {ex.Message}"
                };
            }
        }

        private Task<PicklistUploadResponse> ProcessPicklistUploadInternalAsync(IXLWorksheet worksheet, CancellationToken cancellationToken)
        {
            var headerRow = worksheet.Row(1);
            var colMap = new Dictionary<string, int>();

            int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (int col = 1; col <= lastCol; col++)
            {
                string header = headerRow.Cell(col).GetString();
                string norm = NormalizeColumn(header);
                if (!string.IsNullOrEmpty(norm) && !colMap.ContainsKey(norm))
                {
                    colMap[norm] = col;
                }
            }

            int getCol(params string[] aliases)
            {
                foreach (var a in aliases)
                {
                    string norm = NormalizeColumn(a);
                    if (colMap.TryGetValue(norm, out int colIdx)) return colIdx;
                }
                return 0;
            }

            int picklistCol = getCol("PICKLISTNO", "PICKLISTNUMBER", "PICKLIST");
            int dateCol = getCol("DATE", "PICKLISTDATE");
            int materialCol = getCol("MATERIAL", "MATERIALNO", "ITEM");
            int descCol = getCol("DESCRIPTION", "DESC");
            int qtyCol = getCol("QTY", "QUANTITY");
            int packCol = getCol("PACK");
            int boxCol = getCol("BOX", "COUNT");

            if (picklistCol == 0 || dateCol == 0)
            {
                return Task.FromResult(new PicklistUploadResponse
                {
                    Success = false,
                    Message = "Missing required columns: Picklist No and Date."
                });
            }

            var items = new List<PicklistDataItem>();
            string firstPicklistNo = string.Empty;
            string firstDate = string.Empty;

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var r = worksheet.Row(row);
                string pNo = r.Cell(picklistCol).GetString().Trim();
                string rawDate = r.Cell(dateCol).GetString().Trim();

                if (string.IsNullOrWhiteSpace(pNo) && string.IsNullOrWhiteSpace(rawDate))
                    continue;

                // Normalize date string (handling ddMMyyyy or date objects)
                string cleanedDate = Regex.Replace(rawDate, @"\D", "");
                if (cleanedDate.Length == 7) cleanedDate = "0" + cleanedDate;

                string formattedDate = rawDate;
                if (DateTime.TryParseExact(cleanedDate, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    formattedDate = parsed.ToString("dd/MM/yyyy");
                }

                // Batch consistency check
                if (string.IsNullOrEmpty(firstPicklistNo))
                {
                    firstPicklistNo = pNo;
                    firstDate = formattedDate;
                }
                else
                {
                    if (pNo != firstPicklistNo)
                    {
                        return Task.FromResult(new PicklistUploadResponse
                        {
                            Success = false,
                            Message = $"Picklist No mismatch at row {row}. Expected '{firstPicklistNo}' but found '{pNo}'."
                        });
                    }

                    if (formattedDate != firstDate)
                    {
                        return Task.FromResult(new PicklistUploadResponse
                        {
                            Success = false,
                            Message = $"Date mismatch at row {row}. Expected '{firstDate}' but found '{formattedDate}'."
                        });
                    }
                }

                int.TryParse(qtyCol > 0 ? r.Cell(qtyCol).GetString() : "0", out int qty);
                int.TryParse(packCol > 0 ? r.Cell(packCol).GetString() : "0", out int pack);
                int.TryParse(boxCol > 0 ? r.Cell(boxCol).GetString() : "0", out int box);

                items.Add(new PicklistDataItem
                {
                    PicklistNo = pNo,
                    Date = formattedDate,
                    Material = materialCol > 0 ? r.Cell(materialCol).GetString().Trim() : string.Empty,
                    Description = descCol > 0 ? r.Cell(descCol).GetString().Trim() : string.Empty,
                    Qty = qty,
                    Pack = pack,
                    Box = box
                });
            }

            if (items.Count == 0)
            {
                return Task.FromResult(new PicklistUploadResponse
                {
                    Success = false,
                    Message = "The Picklist file contains no valid data."
                });
            }

            return Task.FromResult(new PicklistUploadResponse
            {
                Success = true,
                Message = $"Picklist '{firstPicklistNo}' validated successfully with {items.Count} items.",
                PicklistNo = firstPicklistNo,
                Date = firstDate,
                TotalRecords = items.Count,
                Items = items
            });
        }

        public async Task<DispatchReportResponse> GetDispatchReportAsync(DispatchReportRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize = request.PageSize < 1 ? 50 : request.PageSize;

                parameters.Add("@status", "DISPATCH_REPORT_DATA", DbType.String);
                parameters.Add("@SearchTerm", request.SearchTerm ?? string.Empty, DbType.String);
                parameters.Add("@fromdate", request.FromDate ?? string.Empty, DbType.String);
                parameters.Add("@todate", request.ToDate ?? string.Empty, DbType.String);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "TRANS_DATE" : request.SortColumn, DbType.String);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection, DbType.String);
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var rows = (await connection.QueryAsync("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure)).ToList();
                int recordCount = parameters.Get<int?>("@RecordCount") ?? rows.Count;

                var dataList = new List<Dictionary<string, object>>();
                foreach (var r in rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in (IDictionary<string, object>)r)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    dataList.Add(dict);
                }

                return new DispatchReportResponse
                {
                    Success = true,
                    Message = "Report fetched successfully",
                    TotalRecords = recordCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SP_NEW_REPORT for DISPATCH_REPORT_DATA");
                return new DispatchReportResponse
                {
                    Success = false,
                    Message = $"Failed to fetch dispatch report: {ex.Message}"
                };
            }
        }

        public async Task<DispatchReportResponse> GetDispatchReportModalAsync(DispatchReportModalRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize = request.PageSize < 1 ? 50 : request.PageSize;

                parameters.Add("@status", "DISPATCH_REPORT_DATA_VIEW", DbType.String);
                parameters.Add("@SearchTerm", request.SearchTerm ?? string.Empty, DbType.String);
                parameters.Add("@fromdate", request.FromDate ?? string.Empty, DbType.String);
                parameters.Add("@todate", request.ToDate ?? string.Empty, DbType.String);
                parameters.Add("@VEHICLE_NO", request.VehicleNo ?? string.Empty, DbType.String);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "TRANS_DATE" : request.SortColumn, DbType.String);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection, DbType.String);
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var rows = (await connection.QueryAsync("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure)).ToList();
                int recordCount = parameters.Get<int?>("@RecordCount") ?? rows.Count;

                var dataList = new List<Dictionary<string, object>>();
                foreach (var r in rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in (IDictionary<string, object>)r)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    dataList.Add(dict);
                }

                return new DispatchReportResponse
                {
                    Success = true,
                    Message = "Modal details fetched successfully",
                    TotalRecords = recordCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SP_NEW_REPORT for DISPATCH_REPORT_DATA_VIEW");
                return new DispatchReportResponse
                {
                    Success = false,
                    Message = $"Failed to fetch modal details: {ex.Message}"
                };
            }
        }
    }
}
