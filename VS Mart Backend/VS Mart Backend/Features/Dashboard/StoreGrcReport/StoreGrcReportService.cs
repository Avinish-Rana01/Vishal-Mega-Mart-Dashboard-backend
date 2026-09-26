using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Features.StoreGrcReport
{
    public class StoreGrcReportService : BaseDashboardService, IStoreGrcReportService
    {
        private readonly ILogger<StoreGrcReportService> _logger;

        public StoreGrcReportService(IConfiguration configuration, IMemoryCache cache, ILogger<StoreGrcReportService> logger)
            : base(configuration, cache)
        {
            _logger = logger;
        }

        public async Task<List<HuNumberItem>> SearchHuNumbersAsync(GrcHuSearchRequest request)
        {
            string cacheKey = $"SearchHuNumbers_{request.GrcStatus}_{request.SearchTerm}_{request.StoreCode}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = "";
                    string grcStatus = request.GrcStatus ?? "";
                    bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                    if (grcStatus == "" || grcStatus == "1")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_GRC" : "BIND_HU_FOR_GRC";
                        parameters.Add("@GRC_STATUS", grcStatus);
                    }
                    else if (grcStatus == "0")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_HHTGRC" : "BIND_HU_FOR_HHTGRC";
                        parameters.Add("@GRC_STATUS", "2");
                    }
                    else if (grcStatus == "2")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_HHTGRC" : "BIND_HU_FOR_HHTGRC";
                        parameters.Add("@GRC_STATUS", "1");
                    }
                    else if (grcStatus == "3")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_STORE_PENDING_GRC" : "BIND_HU_FOR_STORE_PENDING_GRC";
                    }
                    else if (grcStatus == "4")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_GRC" : "BIND_HU_FOR_GRC";
                    }

                    parameters.Add("@status", status, DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@FromDate", request.FromDate ?? "", DbType.String, size: 20);
                    parameters.Add("@Todate", request.ToDate ?? "", DbType.String, size: 20);
                    parameters.Add("@Store_Code", request.StoreCode ?? "", DbType.String, size: 50);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("HU") && x["HU"] != null)
                        .Select(x => new HuNumberItem { Id = x["HU"].ToString()!, Text = x["HU"].ToString()! })
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching HU numbers");
                    return new List<HuNumberItem>();
                }
            });
        }

        public async Task<GrcDetailsResponse> GetGrcDetailsAsync(GrcDetailsRequest request)
        {
            // Direct DB call — SP handles pagination natively with actual pageIndex/pageSize.
            // The old master cache (PageSize=1000) caused a heavy initial fetch on every cache miss.
            return await QueryGrcDetailsFromDbAsync(request);
        }

        private async Task<GrcDetailsResponse> QueryGrcDetailsFromDbAsync(GrcDetailsRequest request)
        {
            try
            {
                var response = new GrcDetailsResponse { PageIndex = request.PageIndex };
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                string grcStatus = request.GrcStatus ?? "";
                
                if (grcStatus == "" || grcStatus == "1")
                {
                    parameters.Add("@status", "SHOW_GRC_DATA");
                    parameters.Add("@GRC_STATUS", grcStatus);
                }
                else if (grcStatus == "0")
                {
                    parameters.Add("@status", "SHOW_HHTGRC_DATA");
                    parameters.Add("@GRC_STATUS", "2");
                }
                else if (grcStatus == "2")
                {
                    parameters.Add("@status", "SHOW_HHTGRC_DATA");
                    parameters.Add("@GRC_STATUS", "1");
                }
                else if (grcStatus == "3")
                {
                    parameters.Add("@status", "SHOW_STORE_PENDING_GRC_DATA");
                }
                else if (grcStatus == "4")
                {
                    parameters.Add("@status", "SHOW_GRC_DATA");
                }

                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@Store_Code", request.StoreName ?? "", DbType.String, size: 50);
                parameters.Add("@FromDate", request.FromDate ?? "", DbType.String, size: 20);
                parameters.Add("@ToDate", request.ToDate ?? "", DbType.String, size: 20);
                parameters.Add("@HU_NO", request.HuNo ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", request.SortColumn ?? "GRC_DATE", DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();
                response.TotalRecords = parameters.Get<int?>("@RecordCount") ?? 0;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC details");
                return new GrcDetailsResponse();
            }
        }

        public async Task<GrcModalDetailsResponse> GetGrcModalDetailsAsync(GrcModalDetailsRequest request)
        {
            // Direct DB call — SP handles pagination natively with actual pageIndex/pageSize.
            // The old master cache (PageSize=1000) caused a heavy initial fetch on every cache miss.
            return await QueryGrcModalDetailsFromDbAsync(request);
        }

        private async Task<GrcModalDetailsResponse> QueryGrcModalDetailsFromDbAsync(GrcModalDetailsRequest request)
        {
            try
            {
                var response = new GrcModalDetailsResponse { PageIndex = request.PageIndex };
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                string grcStatus = request.GrcStatus ?? "";
                
                if (grcStatus == "" || grcStatus == "1")
                {
                    parameters.Add("@status", "VIEW_SHOW_GRC_DATA");
                    parameters.Add("@GRC_STATUS", grcStatus);
                }
                else if (grcStatus == "0")
                {
                    parameters.Add("@status", "VIEW_SHOW_HHTGRC_DATA");
                    parameters.Add("@GRC_STATUS", "2");
                }
                else if (grcStatus == "2")
                {
                    parameters.Add("@status", "VIEW_SHOW_HHTGRC_DATA");
                    parameters.Add("@GRC_STATUS", "1");
                }
                else if (grcStatus == "3")
                {
                    parameters.Add("@status", "VIEW_SHOW_STORE_PENDING_GRC_DATA");
                }
                else if (grcStatus == "4")
                {
                    parameters.Add("@status", "VIEW_SHOW_GRC_DATA");
                }

                string effectiveSearch = !string.IsNullOrEmpty(request.SearchTerm) 
                    ? request.SearchTerm 
                    : (request.Article ?? "");
                parameters.Add("@SearchTerm", effectiveSearch, DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@Store_Code", request.StoreCode ?? "", DbType.String, size: 50);
                parameters.Add("@HU_NO", request.HuNumber ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", request.SortColumn ?? "GRC_DATE", DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);

                string dateStr = !string.IsNullOrWhiteSpace(request.Date) ? request.Date.Trim() : "";
                string formattedDate = "";
                if (!string.IsNullOrWhiteSpace(dateStr))
                {
                    string[] formats = { "dd-MM-yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MMM-yyyy" };
                    if (DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime exactDate))
                    {
                        formattedDate = exactDate.ToString("yyyy-MM-dd");
                    }
                    else if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        formattedDate = parsedDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        formattedDate = dateStr;
                    }
                }

                parameters.Add("@FromDate", formattedDate, DbType.String);
                parameters.Add("@ToDate", formattedDate, DbType.String);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@MATERIALCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.TotalRecords = parameters.Get<int?>("@RecordCount") ?? 0;
                response.Qty = parameters.Get<int?>("@QTY") ?? 0;
                response.MaterialCount = parameters.Get<int?>("@MATERIALCOUNT") ?? 0;
                response.ActualQty = parameters.Get<int?>("@ACTUALQTY") ?? 0;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC modal details");
                return new GrcModalDetailsResponse();
            }
        }

        public async Task<StoreDashboardResponse> GetStoreGrcReportAsync(StoreGrcReportQueryRequest request)
        {
            // Direct DB call — SP handles pagination natively with actual pageIndex/pageSize.
            // The old master cache (PageSize=1000) caused a heavy initial fetch on every cache miss.
            return await QueryStoreGrcReportFromDbAsync(request);
        }

        private async Task<StoreDashboardResponse> QueryStoreGrcReportFromDbAsync(StoreGrcReportQueryRequest request)
        {
            try
            {
                var response = new StoreDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "LAST7DAY_STORE_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.StoreCode ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@Store_Code", request.StoreCode ?? "", DbType.String, size: 50);
                parameters.Add("@fromdate", request.FromDate ?? "", DbType.String, size: 20);
                parameters.Add("@todate", request.ToDate ?? "", DbType.String, size: 20);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "GRC_DATE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_WRONG_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HHT_VALIDATE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new StoreDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    HuReceivedQty = parameters.Get<int?>("@QTY") ?? 0,
                    HuValidatedQty = parameters.Get<int?>("@HU_VALIDATED_QTY") ?? 0,
                    HuWrongQty = parameters.Get<int?>("@HU_WRONG_QTY") ?? 0,
                    HhtValidateQty = parameters.Get<int?>("@HHT_VALIDATE_QTY") ?? 0,
                    EncodedQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0,
                    StoreName = response.Items.Count > 0 && response.Items[0].ContainsKey("STORE_NAME") ? response.Items[0]["STORE_NAME"]?.ToString() : request.StoreCode,
                    Date = request.FromDate
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store GRC report");
                return new StoreDashboardResponse();
            }
        }
    }
}
