using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;

namespace VS_Mart_Backend.Features.MainDashboard
{
    public class MainDashboardService : BaseDashboardService, IMainDashboardService
    {
        public MainDashboardService(IConfiguration configuration, IMemoryCache cache)
            : base(configuration, cache)
        {
        }

        public async Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request)
        {
            string cacheKey = $"LiveStockDetails_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new LiveStockResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                
                int userIdInt = 0;
                int.TryParse(request.UserId, out userIdInt);

                parameters.Add("@status", "LIVE_STOCK_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@User_ID", userIdInt, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new LiveStockSummary
                {
                    PageIndex = request.PageIndex,
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    SapQty = parameters.Get<int?>("@QTY") ?? 0,
                    RfidQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0,
                    DiffQty = parameters.Get<int?>("@DIFF_QTY") ?? 0,
                    StoreName = response.Items.Count > 0 && response.Items[0].ContainsKey("STORE_NAME") ? response.Items[0]["STORE_NAME"]?.ToString() : null,
                    Date = null
                };
                return response;
            });
        }

        public async Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request)
        {
            string cacheKey = $"TagCycleCount_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new TagCycleCountResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "TAG_CYCLE_COUNT", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "CYCLE_COUNT" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "DESC", DbType.String, size: 10);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using var multi = await connection.QueryMultipleAsync("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                
                var items = await multi.ReadAsync<dynamic>();
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                if (!multi.IsConsumed)
                {
                    var distItems = await multi.ReadAsync<dynamic>();
                    response.Distribution = distItems.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();
                }

                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                int qty = parameters.Get<int?>("@QTY") ?? 0;

                double exactAverage = 0;
                int roundedAverage = 0;

                if (recordCount > 0)
                {
                    exactAverage = (double)qty / recordCount;
                    roundedAverage = Convert.ToInt32(Math.Round(exactAverage, 0, MidpointRounding.AwayFromZero));
                }

                response.Summary = new TagCycleCountSummary
                {
                    RecordCount = recordCount,
                    Qty = qty,
                    ExactAverage = exactAverage,
                    AvgTagPercentage = roundedAverage
                };
                return response;
            });
        }

        public async Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request)
        {
            string cacheKey = $"StoreDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new StoreDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "STORE_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@User_ID", request.UserId ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "Store" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_WRONG_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HHT_VALIDATE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new StoreDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    HuReceivedQty = parameters.Get<int?>("@QTY") ?? 0,
                    HuValidatedQty = parameters.Get<int?>("@HU_VALIDATED_QTY") ?? 0,
                    HuWrongQty = parameters.Get<int?>("@HU_WRONG_QTY") ?? 0,
                    HhtValidateQty = parameters.Get<int?>("@HHT_VALIDATE_QTY") ?? 0,
                    EncodedQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0
                };
                return response;
            });
        }

        public async Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request)
        {
            string cacheKey = $"SaleDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new SaleDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "SALE_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@User_ID", request.UserId ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DPOS_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RFID_CHECKOUT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RFID_DPOS_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@MATCHING_WITH_DPOS_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NOT_MATCHING_WITH_DPOS_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NOT_MATCHING_WITH_RFID_CHECKOUT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TAFFETA_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@MANUAL_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DPOS_VOID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RFID_VOID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFF_VOID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new SaleDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalDposSale = parameters.Get<int?>("@DPOS_SALE") ?? 0,
                    TotalRfidCheckout = parameters.Get<int?>("@RFID_CHECKOUT") ?? 0,
                    TotalDposRfidSale = parameters.Get<int?>("@RFID_DPOS_SALE") ?? 0,
                    TotalRfidCheckoutMatch = parameters.Get<int?>("@MATCHING_WITH_DPOS_SALE") ?? 0,
                    TotalRfidCheckoutNotMatch = parameters.Get<int?>("@NOT_MATCHING_WITH_DPOS_SALE") ?? 0,
                    TotalPosSaleNotMatch = parameters.Get<int?>("@NOT_MATCHING_WITH_RFID_CHECKOUT") ?? 0,
                    TotalTaffetaSale = parameters.Get<int?>("@TAFFETA_SALE") ?? 0,
                    TotalManualSale = parameters.Get<int?>("@MANUAL_SALE") ?? 0,
                    TotalVoid = parameters.Get<int?>("@DPOS_VOID") ?? 0,
                    TotalRfidCheckoutMatchDpos = parameters.Get<int?>("@RFID_VOID") ?? 0,
                    TotalDiffVoid = parameters.Get<int?>("@DIFF_VOID") ?? 0
                };
                return response;
            });
        }

        public async Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request)
        {
            string cacheKey = $"ReturnDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new ReturnDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "RETURN_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@User_ID", request.UserId ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new ReturnDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    ReturnQty = parameters.Get<int?>("@QTY") ?? 0,
                    ReturnEncodedQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0,
                    PendingQty = parameters.Get<int?>("@DIFF_QTY") ?? 0
                };
                return response;
            });
        }

        public async Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request)
        {
            string cacheKey = $"VoidDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new VoidDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "VOID_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@User_ID", request.UserId ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new VoidDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    ReturnQty = parameters.Get<int?>("@QTY") ?? 0,
                    ReturnEncodedQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0,
                    PendingQty = parameters.Get<int?>("@DIFF_QTY") ?? 0
                };
                return response;
            });
        }

        public async Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request)
        {
            string cacheKey = $"DcValidation_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new DcValidateDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@Status", "DC_VALIDATE_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@USER_ID", request.UserId ?? "", DbType.String, size: 50);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "Store" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@UNPROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PROCESSED_ARTICLE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new DcValidateDashboardSummary
                {
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    ProcessedHu = parameters.Get<int?>("@PROCESSED_HU") ?? 0,
                    UnprocessedHu = parameters.Get<int?>("@UNPROCESSED_HU") ?? 0,
                    ArticleQty = parameters.Get<int?>("@PROCESSED_ARTICLE_QTY") ?? 0
                };
                return response;
            });
        }

        public async Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request)
        {
            string cacheKey = $"CycleCountDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new CycleCountDashboardResponse();
                using var connection = new SqlConnection(_connectionString);
                
                int userId = 0;
                int.TryParse(request.UserId, out userId);

                var parameters = new DynamicParameters();
                parameters.Add("@status", "CYCLE_COUNT_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@USER_ID", userId, DbType.Int32);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE CODE" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", request.SortDirection ?? "ASC", DbType.String, size: 10);
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.Summary = new CycleCountDashboardSummary
                {
                    PageIndex = request.PageIndex,
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    RefNo = parameters.Get<int?>("@QTY") ?? 0
                };

                var parameters2 = new DynamicParameters();
                parameters2.Add("@status", "CYCLE_COUNT_GRAPH_DATA", DbType.String, size: 50);
                parameters2.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters2.Add("@PageIndex", request.PageIndex, DbType.Int32);
                parameters2.Add("@PageSize", request.PageSize, DbType.Int32);
                parameters2.Add("@Store_code", "", DbType.String, size: 50);
                parameters2.Add("@fromdate", "", DbType.String, size: 20);
                parameters2.Add("@todate", "", DbType.String, size: 20);
                parameters2.Add("@ref_No", "", DbType.String, size: 50);
                parameters2.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE_CODE" : request.SortColumn, DbType.String, size: 50);
                parameters2.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                parameters2.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters2.Add("@Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var graphItems = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters2, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                var graphDataRows = graphItems.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                foreach (var mainRow in response.Items)
                {
                    if (mainRow.TryGetValue("STORE_NAME", out var storeNameObj) && storeNameObj is string storeName && 
                        mainRow.TryGetValue("DATE", out var dateObj))
                    {
                        DateTime? mainDate = null;
                        if (dateObj is DateTime dt) mainDate = dt.Date;
                        else if (dateObj is string ds && DateTime.TryParse(ds, out var d1)) mainDate = d1.Date;

                        var match = graphDataRows.Find(g => 
                        {
                            bool storeMatch = false;
                            if (g.TryGetValue("STORE", out var gStoreObj) && gStoreObj is string gStore)
                                storeMatch = gStore.Equals(storeName, StringComparison.OrdinalIgnoreCase);
                            
                            bool dateMatch = false;
                            if (g.TryGetValue("LAST_PI_DATE", out var gDateObj))
                            {
                                DateTime? graphDate = null;
                                if (gDateObj is DateTime gd1) graphDate = gd1.Date;
                                else if (gDateObj is string gd2 && DateTime.TryParseExact(gd2, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var d2)) graphDate = d2.Date;
                                else if (gDateObj is string gd3 && DateTime.TryParse(gd3, out var d3)) graphDate = d3.Date;
                                
                                dateMatch = mainDate.HasValue && graphDate.HasValue && graphDate.Value.Date == mainDate.Value.Date;
                            }
                            return storeMatch && dateMatch;
                        });

                        if (match != null)
                        {
                            string[] keysToMerge = { "NO_OF_ARTICLE", "SYSTEM_STOCK", "SCANNED_QTY", "NET_DIFF", "SHORT_QTY", "EXCESS_QTY" };
                            foreach (var key in keysToMerge)
                            {
                                if (match.ContainsKey(key))
                                {
                                    string destKey = key == "NO_OF_ARTICLE" ? "NO_OF_ARTICLES" : 
                                                     key == "NET_DIFF" ? "NET_DIFFERENCE" : key;
                                    mainRow[destKey] = match[key];
                                }
                            }
                        }
                    }
                }
                return response;
            });
        }

        public async Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request)
        {
            try
            {
                string cacheKey = $"VendorHUDiscrepancy_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
                return await GetOrCreateWithSWRAsync(cacheKey, async () =>
                {
                    var response = new VendorHUDiscrepancyResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    int userId = 0;
                    int.TryParse(request.UserId, out userId);

                    parameters.Add("@Status", "HU_DISCREPANCY_VENDOR_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@USER_ID", userId, DbType.Int32);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DIFF_TILL_DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
                    parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_SCANNEDQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIFF_TILL_DATE", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                    response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new VendorHUDiscrepancySummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        ActualQty = parameters.Get<int?>("@HU_DIS_ACTUALQTY") ?? 0,
                        ScannedQty = parameters.Get<int?>("@HU_DIS_SCANNEDQTY") ?? 0,
                        DifferenceQty = parameters.Get<int?>("@HU_DIS_DIFFQTY") ?? 0,
                        DifferenceQtyTillDate = parameters.Get<int?>("@HU_DIFF_TILL_DATE") ?? 0
                    };
                    return response;
                });
            }
            catch (Exception)
            {
                return new VendorHUDiscrepancyResponse();
            }
        }

        public async Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request)
        {
            try
            {
                string cacheKey = $"TagManagementLocation";
                return await GetOrCreateWithSWRAsync(cacheKey, async () =>
                {
                    var response = new TagManagementResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "TAG_MANAGEMENT_LOCATION", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", 1, DbType.Int32);
                    parameters.Add("@PageSize", 100, DbType.Int32);
                    parameters.Add("@SortColumn", "", DbType.String, size: 50);
                    parameters.Add("@SortDirection", "asc", DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@STORECOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@WHCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                    response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new TagManagementSummary
                    {
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        StoreCount = parameters.Get<int?>("@STORECOUNT") ?? 0,
                        WarehouseCount = parameters.Get<int?>("@WHCOUNT") ?? 0
                    };
                    return response;
                });
            }
            catch (Exception)
            {
                return new TagManagementResponse();
            }
        }

        public async Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request)
        {
            try
            {
                string cacheKey = $"WarehouseEncoding_{request.FromDate}_{request.ToDate}";
                return await GetOrCreateWithSWRAsync(cacheKey, async () =>
                {
                    var response = new WarehouseEncodingResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "SHOW_WAREHOUSE_ENCODE_DATA", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.FromDate, DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate, DbType.String, size: 20);
                    parameters.Add("@User_ID", "", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                    parameters.Add("@SortColumn", "", DbType.String, size: 50);
                    parameters.Add("@SortDirection", "", DbType.String, size: 10);

                    parameters.Add("@8TO9", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@9TO10", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@10TO11", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@11TO12", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@12TO13", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@13TO14", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@14TO15", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@15TO16", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@16TO17", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@17TO18", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@18TO19", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@19TO20", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    parameters.Add("@8TO9_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@9TO10_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@10TO11_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@11TO12_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@12TO13_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@13TO14_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@14TO15_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@15TO16_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@16TO17_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@17TO18_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@18TO19_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@19TO20_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    
                    parameters.Add("@MRGQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@EVNQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ERRQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@AVGQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@T_ENC_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@T_ENC_USERS", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                    response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new WarehouseEncodingSummary
                    {
                        Hour8To9 = parameters.Get<int?>("@8TO9") ?? 0,
                        Hour9To10 = parameters.Get<int?>("@9TO10") ?? 0,
                        Hour10To11 = parameters.Get<int?>("@10TO11") ?? 0,
                        Hour11To12 = parameters.Get<int?>("@11TO12") ?? 0,
                        Hour12To13 = parameters.Get<int?>("@12TO13") ?? 0,
                        Hour13To14 = parameters.Get<int?>("@13TO14") ?? 0,
                        Hour14To15 = parameters.Get<int?>("@14TO15") ?? 0,
                        Hour15To16 = parameters.Get<int?>("@15TO16") ?? 0,
                        Hour16To17 = parameters.Get<int?>("@16TO17") ?? 0,
                        Hour17To18 = parameters.Get<int?>("@17TO18") ?? 0,
                        Hour18To19 = parameters.Get<int?>("@18TO19") ?? 0,
                        Hour19To20 = parameters.Get<int?>("@19TO20") ?? 0
                    };
                    return response;
                });
            }
            catch (Exception)
            {
                return new WarehouseEncodingResponse();
            }
        }
    }
}
