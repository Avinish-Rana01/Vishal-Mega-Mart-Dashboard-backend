using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
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

        private static bool MatchStore(IDictionary<string, object?> row, IEnumerable<string>? storeCodes)
        {
            if (storeCodes == null) return false;

            var codesSet = storeCodes as ISet<string> ?? new HashSet<string>(storeCodes, StringComparer.OrdinalIgnoreCase);
            if (codesSet.Count == 0) return false;

            string?[] candidates = {
                row.TryGetValue("STORE_CODE", out var sc) && sc != null ? sc.ToString()?.Trim() : null,
                row.TryGetValue("STORE", out var st) && st != null ? st.ToString()?.Trim() : null,
                row.TryGetValue("Store_Code", out var sc2) && sc2 != null ? sc2.ToString()?.Trim() : null,
                row.TryGetValue("Reciving_Plant", out var rp) && rp != null ? rp.ToString()?.Trim() : null
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && codesSet.Contains(candidate))
                    return true;
            }

            return false;
        }

        private static bool MatchStore(IDictionary<string, object?> row, string storeCode)
        {
            if (string.IsNullOrWhiteSpace(storeCode)) return false;
            return MatchStore(row, new[] { storeCode.Trim() });
        }

        private async Task<LiveStockResponse> QueryLiveStockFromDbAsync(LiveStockQueryRequest request, int userIdInt, CancellationToken cancellationToken = default)
        {
            var response = new LiveStockResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            
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

            var cmd = new CommandDefinition("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
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
        }

        public async Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            // Universal Master Cache Key: holds company-wide data for all stores in RAM
            string masterCacheKey = $"LiveStockDetails_Master_{request.SearchTerm}_{request.SortColumn}_{request.SortDirection}_{request.SortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new LiveStockQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QueryLiveStockFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, cancellationToken: cancellationToken);

            // 1. Super Admin: serve company-wide master dataset (instantly from RAM)
            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new LiveStockResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            // 2. User assigned to authorized store(s): serve sliced in-memory data
            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items
                .Where(x => MatchStore(x, storeCodes))
                .ToList();

            int sapQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("SAP_STOCK", out var s) && s != null ? s : 0));
            int rfidQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("RFID_STOCK", out var r) && r != null ? r : 0));
            int diffQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("DIFFERENCE", out var d) && d != null ? d : 0));
            string? storeName = storeItems.Count == 1 && storeItems[0].TryGetValue("STORE_NAME", out var sn) == true 
                ? sn?.ToString() 
                : (storeItems.Count > 1 ? $"{storeItems.Count} Assigned Stores" : profile.StoreName);

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new LiveStockResponse
            {
                Items = pagedStoreItems,
                Summary = new LiveStockSummary
                {
                    PageIndex = request.PageIndex,
                    RecordCount = storeItems.Count,
                    TotalCount = storeItems.Count,
                    SapQty = sapQty,
                    RfidQty = rfidQty,
                    DiffQty = diffQty,
                    StoreName = !string.IsNullOrEmpty(storeName) ? storeName : profile.StoreCode,
                    Date = null
                }
            };
        }

        public async Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            int pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
            int pageSize = request.PageSize > 0 ? request.PageSize : 100;
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "CYCLE_COUNT" : request.SortColumn;
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "DESC" : request.SortDirection;
            string searchTerm = request.SearchTerm ?? "";

            string masterCacheKey = $"TagCycleCount_Master_{searchTerm.Trim().ToLowerInvariant()}_{sortCol.Trim().ToLowerInvariant()}_{sortDir.Trim().ToLowerInvariant()}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var response = new TagCycleCountResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", "TAG_CYCLE_COUNT", DbType.String, size: 50);
                parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 1000, DbType.Int32);
                parameters.Add("@SortColumn", sortCol, DbType.String, size: 50);
                parameters.Add("@SortDirection", sortDir, DbType.String, size: 10);
                int userIdVal = 0;
                int.TryParse(request.UserId, out userIdVal);
                parameters.Add("@User_ID", userIdVal, DbType.Int32);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
                using var multi = await connection.QueryMultipleAsync(cmd);
                
                // First result set from SP_NEW_REPORT contains raw individual tag items; discard to reach distribution result set
                await multi.ReadAsync<dynamic>();

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
            }, forceRefresh, customTtl: GetSlowMovingRefreshInterval(), customStaleDuration: GetSlowMovingRefreshInterval(), cancellationToken: cancellationToken);

            return masterData;
        }

        private async Task<StoreDashboardResponse> QueryStoreDashboardFromDbAsync(StoreDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new StoreDashboardResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@status", "STORE_DASHBOARD", DbType.String, size: 50);
            parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
            parameters.Add("@PageSize", request.PageSize, DbType.Int32);
            parameters.Add("@User_ID", userId, DbType.Int32);
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

            var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
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
        }

        public async Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
            string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType.Trim().ToLowerInvariant();

            string masterCacheKey = $"StoreDashboard_Master_{searchTerm}_{sortCol}_{sortDir}_{sortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new StoreDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QueryStoreDashboardFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, cancellationToken: cancellationToken);

            // 1. Super Admin: serve company-wide master dataset (instantly from RAM)
            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new StoreDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            // 2. User assigned to authorized store(s): serve sliced in-memory data
            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            int huReceived = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("HU_RECEIVED_QTY", out var v) && v != null ? v : 0));
            int huValidated = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("HU_VALIDATED_QTY", out var v) && v != null ? v : 0));
            int huWrong = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("HU_WRONG_QTY", out var v) && v != null ? v : 0));
            int hhtValidate = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("HHT_VALIDATE_QTY", out var v) && v != null ? v : 0));
            string? storeName = storeItems.Count == 1 && storeItems[0].TryGetValue("STORE_NAME", out var sn) == true 
                ? sn?.ToString() 
                : (storeItems.Count > 1 ? $"{storeItems.Count} Assigned Stores" : profile.StoreName);

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new StoreDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new StoreDashboardSummary
                {
                    RecordCount = storeItems.Count,
                    TotalCount = storeItems.Count,
                    HuReceivedQty = huReceived,
                    HuValidatedQty = huValidated,
                    HuWrongQty = huWrong,
                    HhtValidateQty = hhtValidate,
                    EncodedQty = 0,
                    StoreName = !string.IsNullOrEmpty(storeName) ? storeName : profile.StoreCode,
                    Date = null
                }
            };
        }

        private async Task<SaleDashboardResponse> QuerySaleDashboardFromDbAsync(SaleDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new SaleDashboardResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@status", "SALE_DASHBOARD", DbType.String, size: 50);
            parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
            parameters.Add("@PageSize", request.PageSize, DbType.Int32);
            parameters.Add("@User_ID", userId, DbType.Int32);
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

            var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
            response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

            // Clamp negative values for Taffeta Sale across rows
            foreach (var row in response.Items)
            {
                if (row.TryGetValue("TOTAL_TAFFETA_SALE", out var taffVal) && taffVal != null)
                {
                    int tVal = Convert.ToInt32(taffVal);
                    if (tVal < 0) row["TOTAL_TAFFETA_SALE"] = 0;
                }
            }

            response.Summary = new SaleDashboardSummary
            {
                RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                TotalDposSale = parameters.Get<int?>("@DPOS_SALE") ?? 0,
                TotalRfidCheckout = parameters.Get<int?>("@RFID_CHECKOUT") ?? 0,
                TotalDposRfidSale = parameters.Get<int?>("@RFID_DPOS_SALE") ?? 0,
                TotalRfidCheckoutMatch = parameters.Get<int?>("@MATCHING_WITH_DPOS_SALE") ?? 0,
                TotalRfidCheckoutNotMatch = parameters.Get<int?>("@NOT_MATCHING_WITH_DPOS_SALE") ?? 0,
                TotalPosSaleNotMatch = parameters.Get<int?>("@NOT_MATCHING_WITH_RFID_CHECKOUT") ?? 0,
                TotalTaffetaSale = Math.Max(0, parameters.Get<int?>("@TAFFETA_SALE") ?? 0),
                TotalManualSale = parameters.Get<int?>("@MANUAL_SALE") ?? 0,
                TotalVoid = parameters.Get<int?>("@DPOS_VOID") ?? 0,
                TotalRfidCheckoutMatchDpos = parameters.Get<int?>("@RFID_VOID") ?? 0,
                TotalDiffVoid = parameters.Get<int?>("@DIFF_VOID") ?? 0
            };
            return response;
        }

        public async Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
            string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType.Trim().ToLowerInvariant();

            string masterCacheKey = $"SaleDashboard_Master_{searchTerm}_{sortCol}_{sortDir}_{sortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new SaleDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QuerySaleDashboardFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, customTtl: GetSlowMovingRefreshInterval(), customStaleDuration: GetSlowMovingRefreshInterval(), cancellationToken: cancellationToken);

            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new SaleDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new SaleDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new SaleDashboardSummary
                {
                    RecordCount = storeItems.Count,
                    TotalDposSale = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_DPOS_SALE", out var v) && v != null ? v : 0)),
                    TotalRfidCheckout = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_RFID_CHECKOUT", out var v) && v != null ? v : 0)),
                    TotalDposRfidSale = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_RFID_DPOS_SALE", out var v) && v != null ? v : 0)),
                    TotalRfidCheckoutMatch = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("RFID_CHECKOUT_MATCHING_WITH_DPOS_SALE", out var v) && v != null ? v : 0)),
                    TotalRfidCheckoutNotMatch = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("RFID_CHECKOUT_NOT_MATCHING_WITH_DPOS_SALE", out var v) && v != null ? v : 0)),
                    TotalPosSaleNotMatch = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("DPOS_SALE_NOT_MATCHING_WITH_RFID_CHECKOUT", out var v) && v != null ? v : 0)),
                    TotalTaffetaSale = Math.Max(0, storeItems.Sum(x => Math.Max(0, Convert.ToInt32(x.TryGetValue("TOTAL_TAFFETA_SALE", out var v) && v != null ? v : 0)))),
                    TotalManualSale = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_MANUAL_SALE", out var v) && v != null ? v : 0)),
                    TotalVoid = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_VOID", out var v) && v != null ? v : 0)),
                    TotalRfidCheckoutMatchDpos = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID", out var v) && v != null ? v : 0)),
                    TotalDiffVoid = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("TOTAL_DIFF_VOID", out var v) && v != null ? v : 0))
                }
            };
        }

        private async Task<ReturnDashboardResponse> QueryReturnDashboardFromDbAsync(ReturnDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new ReturnDashboardResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@status", "RETURN_DASHBOARD", DbType.String, size: 50);
            parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
            parameters.Add("@PageSize", request.PageSize, DbType.Int32);
            parameters.Add("@User_ID", userId, DbType.Int32);
            parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
            parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
            parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

            parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
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
        }

        public async Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
            string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType.Trim().ToLowerInvariant();

            string masterCacheKey = $"ReturnDashboard_Master_{searchTerm}_{sortCol}_{sortDir}_{sortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new ReturnDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QueryReturnDashboardFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, customTtl: GetSlowMovingRefreshInterval(), customStaleDuration: GetSlowMovingRefreshInterval(), cancellationToken: cancellationToken);

            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new ReturnDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ReturnDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new ReturnDashboardSummary
                {
                    RecordCount = storeItems.Count,
                    TotalCount = storeItems.Count,
                    ReturnQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("RETURN_QTY", out var v) && v != null ? v : 0)),
                    ReturnEncodedQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("ENCODE_QTY", out var v) && v != null ? v : 0)),
                    PendingQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("DIFFERENCE_QTY", out var v) && v != null ? v : 0))
                }
            };
        }

        private async Task<VoidDashboardResponse> QueryVoidDashboardFromDbAsync(VoidDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new VoidDashboardResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@status", "VOID_DASHBOARD", DbType.String, size: 50);
            parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
            parameters.Add("@PageSize", request.PageSize, DbType.Int32);
            parameters.Add("@User_ID", userId, DbType.Int32);
            parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn, DbType.String, size: 50);
            parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
            parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

            parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
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
        }

        public async Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
            string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType.Trim().ToLowerInvariant();

            string masterCacheKey = $"VoidDashboard_Master_{searchTerm}_{sortCol}_{sortDir}_{sortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new VoidDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QueryVoidDashboardFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, customTtl: GetSlowMovingRefreshInterval(), customStaleDuration: GetSlowMovingRefreshInterval(), cancellationToken: cancellationToken);

            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new VoidDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new VoidDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new VoidDashboardSummary
                {
                    RecordCount = storeItems.Count,
                    TotalCount = storeItems.Count,
                    ReturnQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("VOID_QTY", out var v) && v != null ? v : 0)),
                    ReturnEncodedQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("ENCODE_QTY", out var v) && v != null ? v : 0)),
                    PendingQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("DIFFERENCE_QTY", out var v) && v != null ? v : 0))
                }
            };
        }

        private async Task<DcValidateDashboardResponse> QueryDcValidateFromDbAsync(DcValidateDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new DcValidateDashboardResponse();
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@Status", "DC_VALIDATE_DASHBOARD", DbType.String, size: 50);
            parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
            parameters.Add("@PageSize", request.PageSize, DbType.Int32);
            parameters.Add("@USER_ID", userId, DbType.Int32);
            parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "Store" : request.SortColumn, DbType.String, size: 50);
            parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);
            parameters.Add("@SortType", request.SortType ?? "string", DbType.String, size: 50);

            parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@PROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@UNPROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@PROCESSED_ARTICLE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd);
            response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

            response.Summary = new DcValidateDashboardSummary
            {
                RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                ProcessedHu = parameters.Get<int?>("@PROCESSED_HU") ?? 0,
                UnprocessedHu = parameters.Get<int?>("@UNPROCESSED_HU") ?? 0,
                ArticleQty = parameters.Get<int?>("@PROCESSED_ARTICLE_QTY") ?? 0
            };
            return response;
        }

        public async Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
            string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType.Trim().ToLowerInvariant();

            string masterCacheKey = $"DcValidation_Master_{searchTerm}_{sortCol}_{sortDir}_{sortType}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new DcValidateDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection,
                    SortType = request.SortType
                };
                return await QueryDcValidateFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, cancellationToken: cancellationToken);

            // 1. Super Admin or Warehouse Admin: both see company-wide master dataset instantly from RAM!
            if (profile.IsSuperAdmin || string.Equals(profile.UserType, "Warehouse Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new DcValidateDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            // 2. User assigned to authorized store(s): serve sliced in-memory data
            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new DcValidateDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new DcValidateDashboardSummary
                {
                    RecordCount = storeItems.Count,
                    ProcessedHu = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("PROCESSED_HU", out var v) && v != null ? v : 0)),
                    UnprocessedHu = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("UNPROCESSED_HU", out var v) && v != null ? v : 0)),
                    ArticleQty = storeItems.Sum(x => Convert.ToInt32(x.TryGetValue("PROCESSED_ARTICLE_QTY", out var v) && v != null ? v : 0))
                }
            };
        }

        private async Task<CycleCountDashboardResponse> QueryCycleCountFromDbAsync(CycleCountDashboardQueryRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var response = new CycleCountDashboardResponse();
            using var connection = new SqlConnection(_connectionString);

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

            var cmd1 = new CommandDefinition("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            var items = await connection.QueryAsync<dynamic>(cmd1);
            response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

            response.Summary = new CycleCountDashboardSummary
            {
                PageIndex = request.PageIndex,
                RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                RefNo = parameters.Get<int?>("@QTY") ?? 0
            };

            var parameters2 = new DynamicParameters();
            parameters2.Add("@status", "CYCLE_COUNT_REPORT_VIEW", DbType.String, size: 50);
            parameters2.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
            parameters2.Add("@Store_code", "", DbType.String, size: 50);
            parameters2.Add("@fromdate", "", DbType.String, size: 20);
            parameters2.Add("@todate", "", DbType.String, size: 20);
            parameters2.Add("@ref_No", "", DbType.String, size: 50);
            parameters2.Add("@USER_ID", userId, DbType.Int32);
            parameters2.Add("@SortColumn", "DATE", DbType.String, size: 50);
            parameters2.Add("@SortDirection", "desc", DbType.String, size: 10);
            parameters2.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters2.Add("@Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var cmd2 = new CommandDefinition("[SP_NEW_REPORT]", parameters2, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
            using var multi = await connection.QueryMultipleAsync(cmd2);
            var graphItems = await multi.ReadAsync<dynamic>();
            var graphDataRows = graphItems.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

            var refIndex = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            var storeIndex = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

            foreach (var g in graphDataRows)
            {
                if (g.TryGetValue("Ref_ID", out var gRef) && gRef != null)
                {
                    string refKey = gRef.ToString()!;
                    if (!string.IsNullOrEmpty(refKey))
                        refIndex.TryAdd(refKey, g);
                }
                if (g.TryGetValue("STORE_CODE", out var gStore) && gStore != null)
                {
                    string storeKey = gStore.ToString()!;
                    if (!string.IsNullOrEmpty(storeKey))
                        storeIndex.TryAdd(storeKey, g);
                }
            }

            foreach (var mainRow in response.Items)
            {
                var mainRefNo = mainRow.TryGetValue("REF_NO", out var refObj) ? refObj?.ToString() : null;
                var mainStoreCode = mainRow.TryGetValue("STORE_CODE", out var scObj) ? scObj?.ToString() : null;

                Dictionary<string, object?>? match = null;
                if (!string.IsNullOrEmpty(mainRefNo) && refIndex.TryGetValue(mainRefNo, out var byRef))
                {
                    match = byRef;
                }
                else if (!string.IsNullOrEmpty(mainStoreCode) && storeIndex.TryGetValue(mainStoreCode, out var byStore))
                {
                    match = byStore;
                }

                if (match != null)
                {
                    string[] keysToMerge = { "NO_OF_ARTICLE", "SYSTEM_STOCK", "SCANNED_QTY", "NET_DIFF", "SHORT_QTY", "EXCESS_QTY" };
                    foreach (var key in keysToMerge)
                    {
                        if (match.TryGetValue(key, out var val))
                        {
                            string destKey = key == "NO_OF_ARTICLE" ? "NO_OF_ARTICLES" :
                                             key == "NET_DIFF" ? "NET_DIFFERENCE" : key;
                            mainRow[destKey] = val;
                        }
                    }
                }

                if (!mainRow.ContainsKey("NO_OF_ARTICLES")) mainRow["NO_OF_ARTICLES"] = 0;
                if (!mainRow.ContainsKey("SYSTEM_STOCK")) mainRow["SYSTEM_STOCK"] = 0;
                if (!mainRow.ContainsKey("SCANNED_QTY")) mainRow["SCANNED_QTY"] = 0;
                if (!mainRow.ContainsKey("NET_DIFFERENCE")) mainRow["NET_DIFFERENCE"] = 0;
                if (!mainRow.ContainsKey("SHORT_QTY")) mainRow["SHORT_QTY"] = 0;
                if (!mainRow.ContainsKey("EXCESS_QTY")) mainRow["EXCESS_QTY"] = 0;
            }

            return response;
        }

        public async Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
            int masterAdminId = await GetActiveSuperAdminIdAsync();

            string searchTerm = (request.SearchTerm ?? "").Trim().ToLowerInvariant();
            string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "store code" : request.SortColumn.Trim().ToLowerInvariant();
            string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();

            string masterCacheKey = $"CycleCountDashboard_Master_{searchTerm}_{sortCol}_{sortDir}";
            var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
            {
                var masterRequest = new CycleCountDashboardQueryRequest
                {
                    UserId = masterAdminId.ToString(),
                    SearchTerm = request.SearchTerm,
                    PageIndex = 1,
                    PageSize = 1000,
                    SortColumn = request.SortColumn,
                    SortDirection = request.SortDirection
                };
                return await QueryCycleCountFromDbAsync(masterRequest, masterAdminId, cancellationToken);
            }, forceRefresh, cancellationToken: cancellationToken);

            if (profile.IsSuperAdmin)
            {
                if (request.PageIndex == 1 && (request.PageSize >= masterData.Items.Count || request.PageSize == 100))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new CycleCountDashboardResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }

            var storeCodes = profile.AuthorizedStoreCodes;
            var storeItems = masterData.Items.Where(x => MatchStore(x, storeCodes)).ToList();

            var pagedStoreItems = storeItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new CycleCountDashboardResponse
            {
                Items = pagedStoreItems,
                Summary = new CycleCountDashboardSummary
                {
                    PageIndex = request.PageIndex,
                    RecordCount = storeItems.Count,
                    RefNo = storeItems.Count
                }
            };
        }

        public async Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            try
            {
                int pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
                int pageSize = request.PageSize > 0 ? request.PageSize : 100;
                string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "DIFF_TILL_DATE" : request.SortColumn;
                string sortDir = string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection;
                string sortType = string.IsNullOrEmpty(request.SortType) ? "string" : request.SortType;
                string searchTerm = request.SearchTerm ?? "";

                // Shared master cache key across all roles (Vendor discrepancy is company-wide by vendor)
                string masterCacheKey = $"VendorHUDiscrepancy_Master_{searchTerm.Trim().ToLowerInvariant()}_{sortCol.Trim().ToLowerInvariant()}_{sortDir.Trim().ToLowerInvariant()}_{sortType.Trim().ToLowerInvariant()}";
                var masterData = await GetOrCreateWithSWRAsync(masterCacheKey, async () =>
                {
                    var response = new VendorHUDiscrepancyResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    int userId = 0;
                    int.TryParse(request.UserId, out userId);

                    parameters.Add("@Status", "HU_DISCREPANCY_VENDOR_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 200);
                    parameters.Add("@PageIndex", 1, DbType.Int32);
                    parameters.Add("@PageSize", 1000, DbType.Int32);
                    parameters.Add("@USER_ID", userId, DbType.Int32);
                    parameters.Add("@SortColumn", sortCol, DbType.String, size: 50);
                    parameters.Add("@SortDirection", sortDir, DbType.String, size: 10);
                    parameters.Add("@SortType", sortType, DbType.String, size: 50);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_SCANNEDQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIS_DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HU_DIFF_TILL_DATE", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
                    var items = await connection.QueryAsync<dynamic>(cmd);
                    response.Items = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new VendorHUDiscrepancySummary
                    {
                        PageIndex = 1,
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        ActualQty = parameters.Get<int?>("@HU_DIS_ACTUALQTY") ?? 0,
                        ScannedQty = parameters.Get<int?>("@HU_DIS_SCANNEDQTY") ?? 0,
                        DifferenceQty = parameters.Get<int?>("@HU_DIS_DIFFQTY") ?? 0,
                        DifferenceQtyTillDate = parameters.Get<int?>("@HU_DIFF_TILL_DATE") ?? 0
                    };
                    return response;
                }, forceRefresh, cancellationToken: cancellationToken);

                if (pageIndex == 1 && (pageSize >= masterData.Items.Count || pageSize == 1000))
                {
                    return masterData;
                }

                var pagedItems = masterData.Items
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new VendorHUDiscrepancyResponse
                {
                    Items = pagedItems,
                    Summary = masterData.Summary
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return new VendorHUDiscrepancyResponse();
            }
        }

        public async Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            try
            {
                int userIdVal = 0;
                int.TryParse(request.UserId, out userIdVal);
                string cacheKey = $"TagManagementLocation_{userIdVal}";
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
                    parameters.Add("@User_ID", userIdVal, DbType.Int32);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@STORECOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@WHCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Newtags", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    // Discard raw individual tag items result set; only summary output parameters are needed by dashboard
                    var cmd = new CommandDefinition("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
                    await connection.QueryAsync<dynamic>(cmd);

                    response.Summary = new TagManagementSummary
                    {
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        StoreCount = parameters.Get<int?>("@STORECOUNT") ?? 0,
                        WarehouseCount = parameters.Get<int?>("@WHCOUNT") ?? 0,
                        NewTags = parameters.Get<int?>("@Newtags") ?? 0,

                    };
                    return response;
                }, forceRefresh, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return new TagManagementResponse();
            }
        }

        public async Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await GetUserProfileAsync(request.UserId, cancellationToken);
                int filterUserId = 0;
                // Only filter by specific encoder if user is a standard non-admin operator
                if (!profile.IsSuperAdmin && 
                    !string.Equals(profile.UserType, "Warehouse Admin", StringComparison.OrdinalIgnoreCase) && 
                    !string.Equals(profile.UserType, "Admin", StringComparison.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(profile.StoreCode))
                {
                    int.TryParse(request.UserId, out filterUserId);
                }

                string cacheKey = $"WarehouseEncoding_{request.FromDate}_{request.ToDate}_{filterUserId}";
                return await GetOrCreateWithSWRAsync(cacheKey, async () =>
                {
                    var response = new WarehouseEncodingResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "SHOW_WAREHOUSE_ENCODE_DATA", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.FromDate, DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate, DbType.String, size: 20);
                    parameters.Add("@User_ID", filterUserId, DbType.Int32);
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

                    var cmd = new CommandDefinition("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken);
                    var items = await connection.QueryAsync<dynamic>(cmd);
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
                }, forceRefresh, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return new WarehouseEncodingResponse();
            }
        }
    }
}
