using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using VS_Mart_Backend.Models;
using System.Collections.Concurrent;

namespace VS_Mart_Backend.Services
{
    public interface ILiveStockService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);
        Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request);

        Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request);
        Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request);
        Task<StoreDashboardResponse> GetStoreGrcReportAsync(StoreGrcReportQueryRequest request);
        Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request);
        Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request);
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request);
        Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request);
        Task<CycleCountReportViewResponse> GetCycleCountReportViewAsync(CycleCountReportViewQueryRequest request);
        Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request);
        Task<CycleCountDetailsResponse> GetCycleCountDetailsAsync(CycleCountDetailsQueryRequest request);
        Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request);
        Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request);
        Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request);
        Task<StoreSaleReportResponse> GetStoreSaleReportAsync(StoreSaleReportQueryRequest request);


        Task<List<DropdownItem>> BindPOSCounterAsync(BindPOSCounterRequest request);
        Task<List<DropdownItem>> SearchArticlesSaleAsync(SearchArticlesSaleRequest request);
        Task<List<DropdownItem>> SearchEANSaleAsync(SearchEANSaleRequest request);
        Task<SaleDataResponse> GetSaleDataAsync(SaleDataQueryRequest request);

        void InvalidateDashboardCache(string userId = "26");
        Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request);
        Task<VoidReconciliationResponse> GetVoidReconciliationDataAsync(VoidReconciliationRequest request);
    }

    public class LiveStockService : ILiveStockService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private static bool? _cacheOverride = null;
        private readonly ConcurrentDictionary<string, bool> _refreshingKeys = new();

        private class CacheItem<T>
        {
            public T Data { get; set; } = default!;
            public DateTime CreatedAt { get; set; }
        }

        private async Task<T> GetOrCreateWithSWRAsync<T>(string cacheKey, Func<Task<T>> databaseQuery)
        {
            if (!IsCacheEnabled()) return await databaseQuery();

            if (_cache.TryGetValue(cacheKey, out CacheItem<T>? cachedItem) && cachedItem != null)
            {
                if (DateTime.UtcNow - cachedItem.CreatedAt > TimeSpan.FromSeconds(100)) // Stale after 1 min 40s
                {
                    if (_refreshingKeys.TryAdd(cacheKey, true))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var freshData = await databaseQuery();
                                _cache.Set(cacheKey, new CacheItem<T> { Data = freshData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromMinutes(2));
                            }
                            finally
                            {
                                _refreshingKeys.TryRemove(cacheKey, out _);
                            }
                        });
                    }
                }
                return cachedItem.Data;
            }

            var initialData = await databaseQuery();
            _cache.Set(cacheKey, new CacheItem<T> { Data = initialData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromMinutes(2));
            return initialData;
        }

        public LiveStockService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public bool IsCacheEnabled()
        {
            if (_cacheOverride.HasValue) return _cacheOverride.Value;
            return _configuration.GetValue<bool>("EnableCache", true);
        }

        public void SetCacheEnabled(bool enabled)
        {
            _cacheOverride = enabled;
        }

        public async Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request)
        {
            string cacheKey = $"LiveStockDetails_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new LiveStockResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("[SP_New_Dashboard]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input Parameters
                        cmd.Parameters.AddWithValue("@status", "LIVE_STOCK_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        int userIdInt = 0;
                        if (!string.IsNullOrEmpty(request.UserId))
                        {
                            int.TryParse(request.UserId, out userIdInt);
                        }
                        cmd.Parameters.AddWithValue("@User_ID", userIdInt);

                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        // Output Parameters
                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                        pRecordCount.Direction = ParameterDirection.Output;

                        var pTotalCount = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
                        pTotalCount.Direction = ParameterDirection.Output;

                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                        pQty.Direction = ParameterDirection.Output;

                        var pEncodedQty = cmd.Parameters.Add("@ENCODED_QTY", SqlDbType.Int);
                        pEncodedQty.Direction = ParameterDirection.Output;

                        var pDiffQty = cmd.Parameters.Add("@DIFF_QTY", SqlDbType.Int);
                        pDiffQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();

                        // Read Data Table Rows asynchronously
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        // Extract Output Parameters after reader completes
                        response.Summary = new LiveStockSummary
                        {
                            PageIndex = request.PageIndex,
                            RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pTotalCount.Value != DBNull.Value && pTotalCount.Value != null ? Convert.ToInt32(pTotalCount.Value) : 0,
                            SapQty = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0,
                            RfidQty = pEncodedQty.Value != DBNull.Value && pEncodedQty.Value != null ? Convert.ToInt32(pEncodedQty.Value) : 0,
                            DiffQty = pDiffQty.Value != DBNull.Value && pDiffQty.Value != null ? Convert.ToInt32(pDiffQty.Value) : 0,
                            StoreName = response.Items.Count > 0 && response.Items[0].ContainsKey("STORE_NAME") ? response.Items[0]["STORE_NAME"]?.ToString() : null,
                            Date = null
                        };
                    }
                }
                return response;
            });
        }


        public async Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request)
        {
            string cacheKey = $"TagCycleCount_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new TagCycleCountResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input Parameters
                        cmd.Parameters.AddWithValue("@status", "TAG_CYCLE_COUNT");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "CYCLE_COUNT" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "DESC");

                        // Output Parameters
                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                        pRecordCount.Direction = ParameterDirection.Output;

                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                        pQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }

                            // Fetch second result set for recycle distribution (1, 2, 3, 4, >=5)
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new Dictionary<string, object?>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);
                                        object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                        row[columnName] = value;
                                    }
                                    response.Distribution.Add(row);
                                }
                            }
                        }

                        int recordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0;
                        int qty = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0;

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
                    }
                }
                return response;
            });
        }

        public async Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request)
        {
            string cacheKey = $"StoreDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new StoreDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_New_Dashboard", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@status", "STORE_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@User_ID", request.UserId ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "Store" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pTotalCount = cmd.Parameters.Add("@TotalCount", SqlDbType.Int); pTotalCount.Direction = ParameterDirection.Output;
                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int); pQty.Direction = ParameterDirection.Output;
                        var pHuValidatedQty = cmd.Parameters.Add("@HU_VALIDATED_QTY", SqlDbType.Int); pHuValidatedQty.Direction = ParameterDirection.Output;
                        var pHuWrongQty = cmd.Parameters.Add("@HU_WRONG_QTY", SqlDbType.Int); pHuWrongQty.Direction = ParameterDirection.Output;
                        var pHhtValidateQty = cmd.Parameters.Add("@HHT_VALIDATE_QTY", SqlDbType.Int); pHhtValidateQty.Direction = ParameterDirection.Output;
                        var pEncodedQty = cmd.Parameters.Add("@ENCODED_QTY", SqlDbType.Int); pEncodedQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new StoreDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pTotalCount.Value != DBNull.Value ? Convert.ToInt32(pTotalCount.Value) : 0,
                            HuReceivedQty = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0,
                            HuValidatedQty = pHuValidatedQty.Value != DBNull.Value ? Convert.ToInt32(pHuValidatedQty.Value) : 0,
                            HuWrongQty = pHuWrongQty.Value != DBNull.Value ? Convert.ToInt32(pHuWrongQty.Value) : 0,
                            HhtValidateQty = pHhtValidateQty.Value != DBNull.Value ? Convert.ToInt32(pHhtValidateQty.Value) : 0,
                            EncodedQty = pEncodedQty.Value != DBNull.Value ? Convert.ToInt32(pEncodedQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<StoreDashboardResponse> GetStoreGrcReportAsync(StoreGrcReportQueryRequest request)
        {
            string cacheKey = $"StoreGrcReport_{request.StoreCode}_{request.FromDate}_{request.ToDate}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new StoreDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_NEW_DASHBOARD", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@status", "LAST7DAY_STORE_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.StoreCode ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);

                        cmd.Parameters.AddWithValue("@Store_Code", request.StoreCode ?? "");
                        cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                        cmd.Parameters.AddWithValue("@todate", request.ToDate ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "GRC_DATE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection);

                        var pRecordCount = new SqlParameter("@RecordCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pQty = new SqlParameter("@QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pHuValidatedQty = new SqlParameter("@HU_VALIDATED_QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pHuWrongQty = new SqlParameter("@HU_WRONG_QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pHhtValidateQty = new SqlParameter("@HHT_VALIDATE_QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pEncodedQty = new SqlParameter("@ENCODED_QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };

                        cmd.Parameters.Add(pRecordCount);
                        cmd.Parameters.Add(pQty);
                        cmd.Parameters.Add(pHuValidatedQty);
                        cmd.Parameters.Add(pHuWrongQty);
                        cmd.Parameters.Add(pHhtValidateQty);
                        cmd.Parameters.Add(pEncodedQty);

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Result Set: The Rows
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        // Output parameters are populated ONLY AFTER the reader is closed
                        response.Summary = new StoreDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            HuReceivedQty = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0,
                            HuValidatedQty = pHuValidatedQty.Value != DBNull.Value ? Convert.ToInt32(pHuValidatedQty.Value) : 0,
                            HuWrongQty = pHuWrongQty.Value != DBNull.Value ? Convert.ToInt32(pHuWrongQty.Value) : 0,
                            HhtValidateQty = pHhtValidateQty.Value != DBNull.Value ? Convert.ToInt32(pHhtValidateQty.Value) : 0,
                            EncodedQty = pEncodedQty.Value != DBNull.Value ? Convert.ToInt32(pEncodedQty.Value) : 0,
                            StoreName = response.Items.Count > 0 && response.Items[0].ContainsKey("STORE_NAME") ? response.Items[0]["STORE_NAME"]?.ToString() : request.StoreCode,
                            Date = request.FromDate
                        };
                    }
                }
                return response;
            });
        }


        public async Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request)
        {
            string cacheKey = $"SaleDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new SaleDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_New_Dashboard", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@status", "SALE_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@User_ID", request.UserId ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pDposSale = cmd.Parameters.Add("@DPOS_SALE", SqlDbType.Int); pDposSale.Direction = ParameterDirection.Output;
                        var pRfidCheckout = cmd.Parameters.Add("@RFID_CHECKOUT", SqlDbType.Int); pRfidCheckout.Direction = ParameterDirection.Output;
                        var pRfidDposSale = cmd.Parameters.Add("@RFID_DPOS_SALE", SqlDbType.Int); pRfidDposSale.Direction = ParameterDirection.Output;
                        var pMatchingWithDposSale = cmd.Parameters.Add("@MATCHING_WITH_DPOS_SALE", SqlDbType.Int); pMatchingWithDposSale.Direction = ParameterDirection.Output;
                        var pNotMatchingWithDposSale = cmd.Parameters.Add("@NOT_MATCHING_WITH_DPOS_SALE", SqlDbType.Int); pNotMatchingWithDposSale.Direction = ParameterDirection.Output;
                        var pNotMatchingWithRfidCheckout = cmd.Parameters.Add("@NOT_MATCHING_WITH_RFID_CHECKOUT", SqlDbType.Int); pNotMatchingWithRfidCheckout.Direction = ParameterDirection.Output;
                        var pTaffetaSale = cmd.Parameters.Add("@TAFFETA_SALE", SqlDbType.Int); pTaffetaSale.Direction = ParameterDirection.Output;
                        var pManualSale = cmd.Parameters.Add("@MANUAL_SALE", SqlDbType.Int); pManualSale.Direction = ParameterDirection.Output;
                        var pDposVoid = cmd.Parameters.Add("@DPOS_VOID", SqlDbType.Int); pDposVoid.Direction = ParameterDirection.Output;
                        var pRfidVoid = cmd.Parameters.Add("@RFID_VOID", SqlDbType.Int); pRfidVoid.Direction = ParameterDirection.Output;
                        var pDiffVoid = cmd.Parameters.Add("@DIFF_VOID", SqlDbType.Int); pDiffVoid.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new SaleDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalDposSale = pDposSale.Value != DBNull.Value ? Convert.ToInt32(pDposSale.Value) : 0,
                            TotalRfidCheckout = pRfidCheckout.Value != DBNull.Value ? Convert.ToInt32(pRfidCheckout.Value) : 0,
                            TotalDposRfidSale = pRfidDposSale.Value != DBNull.Value ? Convert.ToInt32(pRfidDposSale.Value) : 0,
                            TotalRfidCheckoutMatch = pMatchingWithDposSale.Value != DBNull.Value ? Convert.ToInt32(pMatchingWithDposSale.Value) : 0,
                            TotalRfidCheckoutNotMatch = pNotMatchingWithDposSale.Value != DBNull.Value ? Convert.ToInt32(pNotMatchingWithDposSale.Value) : 0,
                            TotalPosSaleNotMatch = pNotMatchingWithRfidCheckout.Value != DBNull.Value ? Convert.ToInt32(pNotMatchingWithRfidCheckout.Value) : 0,
                            TotalTaffetaSale = pTaffetaSale.Value != DBNull.Value ? Convert.ToInt32(pTaffetaSale.Value) : 0,
                            TotalManualSale = pManualSale.Value != DBNull.Value ? Convert.ToInt32(pManualSale.Value) : 0,
                            TotalVoid = pDposVoid.Value != DBNull.Value ? Convert.ToInt32(pDposVoid.Value) : 0,
                            TotalRfidCheckoutMatchDpos = pRfidVoid.Value != DBNull.Value ? Convert.ToInt32(pRfidVoid.Value) : 0,
                            TotalDiffVoid = pDiffVoid.Value != DBNull.Value ? Convert.ToInt32(pDiffVoid.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request)
        {
            string cacheKey = $"ReturnDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new ReturnDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_New_Dashboard", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@status", "RETURN_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@User_ID", request.UserId ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pTotalCount = cmd.Parameters.Add("@TotalCount", SqlDbType.Int); pTotalCount.Direction = ParameterDirection.Output;
                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int); pQty.Direction = ParameterDirection.Output;
                        var pEncodedQty = cmd.Parameters.Add("@ENCODED_QTY", SqlDbType.Int); pEncodedQty.Direction = ParameterDirection.Output;
                        var pDiffQty = cmd.Parameters.Add("@DIFF_QTY", SqlDbType.Int); pDiffQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new ReturnDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pTotalCount.Value != DBNull.Value ? Convert.ToInt32(pTotalCount.Value) : 0,
                            ReturnQty = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0,
                            ReturnEncodedQty = pEncodedQty.Value != DBNull.Value ? Convert.ToInt32(pEncodedQty.Value) : 0,
                            PendingQty = pDiffQty.Value != DBNull.Value ? Convert.ToInt32(pDiffQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request)
        {
            string cacheKey = $"VoidDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new VoidDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_New_Dashboard", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@status", "VOID_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@User_ID", request.UserId ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pTotalCount = cmd.Parameters.Add("@TotalCount", SqlDbType.Int); pTotalCount.Direction = ParameterDirection.Output;
                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int); pQty.Direction = ParameterDirection.Output;
                        var pEncodedQty = cmd.Parameters.Add("@ENCODED_QTY", SqlDbType.Int); pEncodedQty.Direction = ParameterDirection.Output;
                        var pDiffQty = cmd.Parameters.Add("@DIFF_QTY", SqlDbType.Int); pDiffQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new VoidDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pTotalCount.Value != DBNull.Value ? Convert.ToInt32(pTotalCount.Value) : 0,
                            ReturnQty = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0,
                            ReturnEncodedQty = pEncodedQty.Value != DBNull.Value ? Convert.ToInt32(pEncodedQty.Value) : 0,
                            PendingQty = pDiffQty.Value != DBNull.Value ? Convert.ToInt32(pDiffQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request)
        {
            string cacheKey = $"DcValidation_{request.UserId}_{request.PageIndex}_{request.PageSize}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new DcValidateDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("SP_New_Dashboard", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Status", "DC_VALIDATE_DASHBOARD");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@USER_ID", request.UserId ?? "");
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "Store" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                        cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pProcessedHu = cmd.Parameters.Add("@PROCESSED_HU", SqlDbType.Int); pProcessedHu.Direction = ParameterDirection.Output;
                        var pUnprocessedHu = cmd.Parameters.Add("@UNPROCESSED_HU", SqlDbType.Int); pUnprocessedHu.Direction = ParameterDirection.Output;
                        var pProcessedArticleQty = cmd.Parameters.Add("@PROCESSED_ARTICLE_QTY", SqlDbType.Int); pProcessedArticleQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new DcValidateDashboardSummary
                        {
                            RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                            ProcessedHu = pProcessedHu.Value != DBNull.Value ? Convert.ToInt32(pProcessedHu.Value) : 0,
                            UnprocessedHu = pUnprocessedHu.Value != DBNull.Value ? Convert.ToInt32(pUnprocessedHu.Value) : 0,
                            ArticleQty = pProcessedArticleQty.Value != DBNull.Value ? Convert.ToInt32(pProcessedArticleQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<CycleCountReportViewResponse> GetCycleCountReportViewAsync(CycleCountReportViewQueryRequest request)
        {
            var response = new CycleCountReportViewResponse();
            string connectionString = _configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input Parameters
                    cmd.Parameters.AddWithValue("@status", "CYCLE_COUNT_REPORT_VIEW");
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn);
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "DESC");

                    if (!string.IsNullOrEmpty(request.FromDate))
                        cmd.Parameters.AddWithValue("@FromDate", request.FromDate);
                    if (!string.IsNullOrEmpty(request.ToDate))
                        cmd.Parameters.AddWithValue("@ToDate", request.ToDate);
                    if (!string.IsNullOrEmpty(request.StoreCode))
                        cmd.Parameters.AddWithValue("@Store_code", request.StoreCode);

                    // Output Parameters
                    var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                    pRecordCount.Direction = ParameterDirection.Output;

                    var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                    pQty.Direction = ParameterDirection.Output;

                    await connection.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            response.Items.Add(row);
                        }
                    }

                    response.Summary = new CycleCountReportViewSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                        RefNo = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0
                    };
                }
            }

            return response;
        }

        public async Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request)
        {
            string cacheKey = $"CycleCountDashboard_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new CycleCountDashboardResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("[SP_New_Dashboard]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        int userId = 0;
                        if (!string.IsNullOrEmpty(request.UserId))
                        {
                            int.TryParse(request.UserId, out userId);
                        }

                        // Input Parameters
                        cmd.Parameters.AddWithValue("@status", "CYCLE_COUNT_DASHBOARD");
                        cmd.Parameters.AddWithValue("@USER_ID", userId);
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE CODE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "ASC");

                        // Output Parameters
                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                        pRecordCount.Direction = ParameterDirection.Output;

                        var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                        pQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new CycleCountDashboardSummary
                        {
                            PageIndex = request.PageIndex,
                            RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                            RefNo = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public async Task<CycleCountDetailsResponse> GetCycleCountDetailsAsync(CycleCountDetailsQueryRequest request)
        {
            string cacheKey = $"CycleCountDetails_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}_{request.StoreCode}_{request.FromDate}_{request.ToDate}_{request.RefNo}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new CycleCountDetailsResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure; cmd.CommandTimeout = 120;

                        // Input Parameters
                        cmd.Parameters.AddWithValue("@status", "CYCLE_COUNT_REPORT");
                        cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                        cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                        cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                        cmd.Parameters.AddWithValue("@Store_code", request.StoreCode ?? "");
                        cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                        cmd.Parameters.AddWithValue("@todate", request.ToDate ?? "");
                        cmd.Parameters.AddWithValue("@ref_No", request.RefNo ?? "");

                        cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE_CODE" : request.SortColumn);
                        cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                        // Output Parameters
                        var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                        var pQty = cmd.Parameters.Add("@Qty", SqlDbType.Int); pQty.Direction = ParameterDirection.Output;
                        var pTtlActQty = cmd.Parameters.Add("@Ttl_Act_QTY", SqlDbType.Int); pTtlActQty.Direction = ParameterDirection.Output;
                        var pSumScannedQty = cmd.Parameters.Add("@Sum_Scanned_Qty", SqlDbType.Int); pSumScannedQty.Direction = ParameterDirection.Output;
                        var pDiffQty = cmd.Parameters.Add("@DIFFQTY", SqlDbType.Int); pDiffQty.Direction = ParameterDirection.Output;
                        var pExcessQty = cmd.Parameters.Add("@Excess_Qty", SqlDbType.Int); pExcessQty.Direction = ParameterDirection.Output;

                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }
                                response.Items.Add(row);
                            }
                        }

                        response.Summary = new CycleCountDetailsSummary
                        {
                            PageIndex = request.PageIndex,
                            RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                            TotalCount = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0,
                            ActualQty = pTtlActQty.Value != DBNull.Value && pTtlActQty.Value != null ? Convert.ToInt32(pTtlActQty.Value) : 0,
                            ScannedQty = pSumScannedQty.Value != DBNull.Value && pSumScannedQty.Value != null ? Convert.ToInt32(pSumScannedQty.Value) : 0,
                            DiffQty = pDiffQty.Value != DBNull.Value && pDiffQty.Value != null ? Convert.ToInt32(pDiffQty.Value) : 0,
                            ExcessQty = pExcessQty.Value != DBNull.Value && pExcessQty.Value != null ? Convert.ToInt32(pExcessQty.Value) : 0
                        };
                    }
                }
                return response;
            });
        }

        public void InvalidateDashboardCache(string userId = "26")
        {
            _cache.Remove($"LiveStockDetails_{userId}__1_100");
            _cache.Remove($"CycleCountDashboard_{userId}__1_100_STORE CODE_ASC");
            _cache.Remove($"VendorHUDiscrepancy_{userId}__1_100_DIFF_TILL_DATE_asc");
            _cache.Remove($"TagManagementLocation");
            _cache.Remove($"WarehouseEncoding_{DateTime.Now:yyyy-MM-dd}_{DateTime.Now:yyyy-MM-dd}");
            _cache.Remove($"TagCycleCount__1_100_CYCLE_COUNT_DESC");
            // New dashboard caches
            _cache.Remove($"StoreDashboard_{userId}__1_100_Store_asc");
            _cache.Remove($"SaleDashboard_{userId}__1_100");
            _cache.Remove($"VoidDashboard_{userId}__1_100");
            _cache.Remove($"ReturnDashboard_{userId}__1_100");
            _cache.Remove($"DcValidation_{userId}_1_100");
        }

        public async Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request)
        {
            string cacheKey = $"VendorHUDiscrepancy_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new VendorHUDiscrepancyResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                int userId = 0;
                int.TryParse(request.UserId, out userId);

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("SP_New_Dashboard", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Status", "HU_DISCREPANCY_VENDOR_DASHBOARD");
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@USER_ID", userId);
                    cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DIFF_TILL_DATE" : request.SortColumn);
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");
                    cmd.Parameters.AddWithValue("@SortType", request.SortType ?? "string");

                    // Output Parameters
                    var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                    pRecordCount.Direction = ParameterDirection.Output;

                    var pActualQty = cmd.Parameters.Add("@HU_DIS_ACTUALQTY", SqlDbType.Int);
                    pActualQty.Direction = ParameterDirection.Output;

                    var pScannedQty = cmd.Parameters.Add("@HU_DIS_SCANNEDQTY", SqlDbType.Int);
                    pScannedQty.Direction = ParameterDirection.Output;

                    var pDiffQty = cmd.Parameters.Add("@HU_DIS_DIFFQTY", SqlDbType.Int);
                    pDiffQty.Direction = ParameterDirection.Output;

                    var pDiffTillDate = cmd.Parameters.Add("@HU_DIFF_TILL_DATE", SqlDbType.Int);
                    pDiffTillDate.Direction = ParameterDirection.Output;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            response.Items.Add(row);
                        }
                    }

                    response.Summary = new VendorHUDiscrepancySummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                        ActualQty = pActualQty.Value != DBNull.Value && pActualQty.Value != null ? Convert.ToInt32(pActualQty.Value) : 0,
                        ScannedQty = pScannedQty.Value != DBNull.Value && pScannedQty.Value != null ? Convert.ToInt32(pScannedQty.Value) : 0,
                        DifferenceQty = pDiffQty.Value != DBNull.Value && pDiffQty.Value != null ? Convert.ToInt32(pDiffQty.Value) : 0,
                        DifferenceQtyTillDate = pDiffTillDate.Value != DBNull.Value && pDiffTillDate.Value != null ? Convert.ToInt32(pDiffTillDate.Value) : 0
                    };
                }
                return response;
            });
        }

        public async Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request)
        {
            string cacheKey = $"TagManagementLocation";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new TagManagementResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("SP_NEW_REPORT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@status", "TAG_MANAGEMENT_LOCATION");
                    cmd.Parameters.AddWithValue("@SearchTerm", "");
                    cmd.Parameters.AddWithValue("@PageIndex", 1);
                    cmd.Parameters.AddWithValue("@PageSize", 100);
                    cmd.Parameters.AddWithValue("@SortColumn", "");
                    cmd.Parameters.AddWithValue("@SortDirection", "asc");

                    var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                    pRecordCount.Direction = ParameterDirection.Output;

                    var pStoreCount = cmd.Parameters.Add("@STORECOUNT", SqlDbType.Int);
                    pStoreCount.Direction = ParameterDirection.Output;

                    var pWHCount = cmd.Parameters.Add("@WHCOUNT", SqlDbType.Int);
                    pWHCount.Direction = ParameterDirection.Output;

                    // Provide missing output parameters required by SP
                    cmd.Parameters.Add("@QTY", SqlDbType.Int).Direction = ParameterDirection.Output;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            response.Items.Add(row);
                        }
                    }

                    response.Summary = new TagManagementSummary
                    {
                        RecordCount = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0,
                        StoreCount = pStoreCount.Value != DBNull.Value ? Convert.ToInt32(pStoreCount.Value) : 0,
                        WarehouseCount = pWHCount.Value != DBNull.Value ? Convert.ToInt32(pWHCount.Value) : 0
                    };
                }
                return response;
            });
        }

        public async Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request)
        {
            string cacheKey = $"WarehouseEncoding_{request.FromDate}_{request.ToDate}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new WarehouseEncodingResponse();
                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("SP_NEW_REPORT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@status", "SHOW_WAREHOUSE_ENCODE_DATA");
                    cmd.Parameters.AddWithValue("@fromdate", request.FromDate);
                    cmd.Parameters.AddWithValue("@todate", request.ToDate);
                    cmd.Parameters.AddWithValue("@User_ID", "");
                    cmd.Parameters.AddWithValue("@SearchTerm", "");
                    cmd.Parameters.AddWithValue("@SortColumn", "");
                    cmd.Parameters.AddWithValue("@SortDirection", "");

                    var p8To9 = cmd.Parameters.Add("@8TO9", SqlDbType.Int); p8To9.Direction = ParameterDirection.Output;
                    var p9To10 = cmd.Parameters.Add("@9TO10", SqlDbType.Int); p9To10.Direction = ParameterDirection.Output;
                    var p10To11 = cmd.Parameters.Add("@10TO11", SqlDbType.Int); p10To11.Direction = ParameterDirection.Output;
                    var p11To12 = cmd.Parameters.Add("@11TO12", SqlDbType.Int); p11To12.Direction = ParameterDirection.Output;
                    var p12To13 = cmd.Parameters.Add("@12TO13", SqlDbType.Int); p12To13.Direction = ParameterDirection.Output;
                    var p13To14 = cmd.Parameters.Add("@13TO14", SqlDbType.Int); p13To14.Direction = ParameterDirection.Output;
                    var p14To15 = cmd.Parameters.Add("@14TO15", SqlDbType.Int); p14To15.Direction = ParameterDirection.Output;
                    var p15To16 = cmd.Parameters.Add("@15TO16", SqlDbType.Int); p15To16.Direction = ParameterDirection.Output;
                    var p16To17 = cmd.Parameters.Add("@16TO17", SqlDbType.Int); p16To17.Direction = ParameterDirection.Output;
                    var p17To18 = cmd.Parameters.Add("@17TO18", SqlDbType.Int); p17To18.Direction = ParameterDirection.Output;
                    var p18To19 = cmd.Parameters.Add("@18TO19", SqlDbType.Int); p18To19.Direction = ParameterDirection.Output;
                    var p19To20 = cmd.Parameters.Add("@19TO20", SqlDbType.Int); p19To20.Direction = ParameterDirection.Output;

                    // Provide remaining dummy output parameters to prevent SP crashing
                    cmd.Parameters.Add("@8TO9_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@9TO10_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@10TO11_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@11TO12_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@12TO13_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@13TO14_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@14TO15_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@15TO16_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@16TO17_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@17TO18_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@18TO19_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@19TO20_ERR", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@MRGQTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@EVNQTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@QTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@ERRQTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@TotalCount", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@RecordCount", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@ENCQTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@AVGQTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@T_ENC_QTY", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@T_ENC_USERS", SqlDbType.Int).Direction = ParameterDirection.Output;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            response.Items.Add(row);
                        }
                    }

                    response.Summary = new WarehouseEncodingSummary
                    {
                        Hour8To9 = p8To9.Value != DBNull.Value ? Convert.ToInt32(p8To9.Value) : 0,
                        Hour9To10 = p9To10.Value != DBNull.Value ? Convert.ToInt32(p9To10.Value) : 0,
                        Hour10To11 = p10To11.Value != DBNull.Value ? Convert.ToInt32(p10To11.Value) : 0,
                        Hour11To12 = p11To12.Value != DBNull.Value ? Convert.ToInt32(p11To12.Value) : 0,
                        Hour12To13 = p12To13.Value != DBNull.Value ? Convert.ToInt32(p12To13.Value) : 0,
                        Hour13To14 = p13To14.Value != DBNull.Value ? Convert.ToInt32(p13To14.Value) : 0,
                        Hour14To15 = p14To15.Value != DBNull.Value ? Convert.ToInt32(p14To15.Value) : 0,
                        Hour15To16 = p15To16.Value != DBNull.Value ? Convert.ToInt32(p15To16.Value) : 0,
                        Hour16To17 = p16To17.Value != DBNull.Value ? Convert.ToInt32(p16To17.Value) : 0,
                        Hour17To18 = p17To18.Value != DBNull.Value ? Convert.ToInt32(p17To18.Value) : 0,
                        Hour18To19 = p18To19.Value != DBNull.Value ? Convert.ToInt32(p18To19.Value) : 0,
                        Hour19To20 = p19To20.Value != DBNull.Value ? Convert.ToInt32(p19To20.Value) : 0
                    };
                }
                return response;
            });
        }

        public async Task<StoreSaleReportResponse> GetStoreSaleReportAsync(StoreSaleReportQueryRequest request)
        {
            string cacheKey = $"StoreSaleReport_{request.UserId}_{request.StoreCode}_{request.FromDate}_{request.ToDate}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                var response = new StoreSaleReportResponse();

                string connectionString = _configuration.GetConnectionString("POS")
                    ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

                using var connection = new SqlConnection(connectionString);

                using var cmd = new SqlCommand("SP_NEW_DASHBOARD", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@status", SqlDbType.VarChar, 50)
                    .Value = "LAST7DAY_SALE_DASHBOARD";

                cmd.Parameters.Add("@SearchTerm", SqlDbType.VarChar, 200)
                    .Value = request.SearchTerm ?? "";

                cmd.Parameters.Add("@PageIndex", SqlDbType.Int)
                    .Value = request.PageIndex;

                cmd.Parameters.Add("@PageSize", SqlDbType.Int)
                    .Value = request.PageSize;

                cmd.Parameters.Add("@Store_Code", SqlDbType.VarChar, 50)
                    .Value = request.StoreCode ?? "";

                cmd.Parameters.Add("@fromdate", SqlDbType.VarChar, 20)
                    .Value = request.FromDate ?? "";

                cmd.Parameters.Add("@todate", SqlDbType.VarChar, 20)
                    .Value = request.ToDate ?? "";

                cmd.Parameters.Add("@SortColumn", SqlDbType.VarChar, 50)
                    .Value = string.IsNullOrEmpty(request.SortColumn)
                        ? "DATE"
                        : request.SortColumn;

                cmd.Parameters.Add("@SortDirection", SqlDbType.VarChar, 10)
                    .Value = string.IsNullOrEmpty(request.SortDirection)
                        ? "desc"
                        : request.SortDirection;

                var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                pRecordCount.Direction = ParameterDirection.Output;

                var pDposSale = cmd.Parameters.Add("@DPOS_SALE", SqlDbType.Int);
                pDposSale.Direction = ParameterDirection.Output;

                var pRfidCheckout = cmd.Parameters.Add("@RFID_CHECKOUT", SqlDbType.Int);
                pRfidCheckout.Direction = ParameterDirection.Output;

                var pTaffetaSale = cmd.Parameters.Add("@TAFFETA_SALE", SqlDbType.Int);
                pTaffetaSale.Direction = ParameterDirection.Output;

                var pManualSale = cmd.Parameters.Add("@MANUAL_SALE", SqlDbType.Int);
                pManualSale.Direction = ParameterDirection.Output;

                await connection.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(
                        StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] =
                            reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    response.Items.Add(row);
                }

                response.Summary = new StoreSaleReportSummary
                {
                    RecordCount = pRecordCount.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(pRecordCount.Value),

                    POSSaleQty = pDposSale.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(pDposSale.Value),

                    RFIDCheckoutQty = pRfidCheckout.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(pRfidCheckout.Value),

                    TaffetaSaleQty = pTaffetaSale.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(pTaffetaSale.Value),

                    ManualSaleQty = pManualSale.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(pManualSale.Value),

                    StoreCode = request.StoreCode ?? "",
                    FromDate = request.FromDate ?? "",
                    ToDate = request.ToDate ?? ""
                };

                return response;
            });
        }


        public async Task<List<DropdownItem>> BindPOSCounterAsync(BindPOSCounterRequest request)
        {
            var list = new List<DropdownItem>();
            string connectionString = _configuration.GetConnectionString("POS") ?? "";
            using (var connection = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                string status = "";
                if (request.ColumnName == "TOTAL_DPOS_SALE") status = "BIND_COUNTER_FOR_POS_SALE";
                else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE") status = "BIND_COUNTER_FOR_RFID_DPOS_SALE";
                else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID") status = "BIND_COUNTER_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);

                cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                cmd.Parameters.AddWithValue("@todate", string.IsNullOrEmpty(request.ToDate) ? request.FromDate : request.ToDate);
                cmd.Parameters.AddWithValue("@store_code", request.Store ?? "");

                await connection.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["COUNTER_NO"] != DBNull.Value)
                        {
                            string val = reader["COUNTER_NO"].ToString() ?? "";
                            list.Add(new DropdownItem { Id = val, Text = val });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<DropdownItem>> SearchArticlesSaleAsync(SearchArticlesSaleRequest request)
        {
            var list = new List<DropdownItem>();
            string connectionString = _configuration.GetConnectionString("POS") ?? "";
            using (var connection = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                string status = "";
                bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                if (request.ColumnName == "TOTAL_DPOS_SALE")
                    status = isSearch ? "SEARCH_BIND_ARTICLE_FOR_POS_SALE" : "BIND_ARTICLE_FOR_POS_SALE";
                else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE")
                    status = isSearch ? "SEARCH_BIND_ARTICLE_FOR_RFID_DPOS_SALE" : "BIND_ARTICLE_FOR_RFID_DPOS_SALE";
                else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID")
                    status = isSearch ? "SEARCH_BIND_MATERIAL_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" : "BIND_MATERIAL_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);

                cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                cmd.Parameters.AddWithValue("@store_code", request.Store ?? "");
                cmd.Parameters.AddWithValue("@COUNTER_NO", request.Pos ?? "");
                cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                cmd.Parameters.AddWithValue("@todate", string.IsNullOrEmpty(request.ToDate) ? request.FromDate : request.ToDate);
                cmd.Parameters.AddWithValue("@User_ID", "0");

                await connection.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["ARTICLE"] != DBNull.Value)
                        {
                            string val = reader["ARTICLE"].ToString() ?? "";
                            list.Add(new DropdownItem { Id = val, Text = val });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<DropdownItem>> SearchEANSaleAsync(SearchEANSaleRequest request)
        {
            var list = new List<DropdownItem>();
            string connectionString = _configuration.GetConnectionString("POS") ?? "";
            using (var connection = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                string status = "";
                bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                if (request.ColumnName == "TOTAL_DPOS_SALE")
                    status = isSearch ? "SEARCH_BIND_EAN_FOR_POS_SALE" : "BIND_EAN_FOR_POS_SALE";
                else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE")
                    status = isSearch ? "SEARCH_BIND_EAN_FOR_RFID_DPOS_SALE" : "BIND_EAN_FOR_RFID_DPOS_SALE";
                else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID")
                    status = isSearch ? "SEARCH_BIND_EAN_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" : "BIND_EAN_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);

                cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                cmd.Parameters.AddWithValue("@store_code", request.Store ?? "");
                cmd.Parameters.AddWithValue("@COUNTER_NO", request.Pos ?? "");
                cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                cmd.Parameters.AddWithValue("@todate", request.ToDate ?? "");
                cmd.Parameters.AddWithValue("@Material", request.Material ?? "");
                cmd.Parameters.AddWithValue("@User_ID", "0");

                await connection.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["EAN"] != DBNull.Value)
                        {
                            string val = reader["EAN"].ToString() ?? "";
                            list.Add(new DropdownItem { Id = val, Text = val });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<SaleDataResponse> GetSaleDataAsync(SaleDataQueryRequest request)
        {
            var response = new SaleDataResponse();
            string connectionString = _configuration.GetConnectionString("POS") ?? "";

            using (var connection = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("[SP_NEW_REPORT]", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure; cmd.CommandTimeout = 120;

                string status = "";
                if (request.ColumnName == "TOTAL_DPOS_SALE") status = "SHOW_POS_SALE_DATA";
                else if (request.ColumnName == "TOTAL_RFID_CHECKOUT") status = "SHOW_RFID_CHECKOUT_DATA";
                else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE") status = "SHOW_RFID_DPOS_SALE_DATA";
                else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_SALE") status = "SHOW_RFID_CHECKOUT_MATCHING_WITH_DPOS_SALE_DATA";
                else if (request.ColumnName == "RFID_CHECKOUT_NOT_MATCHING_WITH_DPOS_SALE") status = "SHOW_RFID_CHECKOUT_NOT_MATCHING_WITH_DPOS_SALE_DATA";
                else if (request.ColumnName == "DPOS_SALE_NOT_MATCHING_WITH_RFID_CHECKOUT") status = "SHOW_DPOS_SALE_NOT_MATCHING_WITH_RFID_CHECKOUT_DATA";
                else if (request.ColumnName == "TOTAL_MANUAL_SALE") status = "SHOW_MANUAL_SALE_DATA";
                else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID") status = "BIND_DATA_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);

                cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                cmd.Parameters.AddWithValue("@store_code", request.StoreName ?? "");
                cmd.Parameters.AddWithValue("@fromdate", request.FromDate ?? "");
                cmd.Parameters.AddWithValue("@todate", request.ToDate ?? "");
                cmd.Parameters.AddWithValue("@COUNTER_NO", request.Pos ?? "");
                cmd.Parameters.AddWithValue("@Material", request.ArticleNo ?? "");
                cmd.Parameters.AddWithValue("@EAN", request.Ean ?? "");
                cmd.Parameters.AddWithValue("@User_ID", request.UserId ?? "0");

                cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "ITEM_CD" : request.SortColumn);
                cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int); pRecordCount.Direction = ParameterDirection.Output;
                var pTotalCount = cmd.Parameters.Add("@TotalCount", SqlDbType.Int); pTotalCount.Direction = ParameterDirection.Output;
                var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int); pQty.Direction = ParameterDirection.Output;

                await connection.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        response.Items.Add(row);
                    }
                }

                response.Summary = new SaleDataSummary
                {
                    PageIndex = request.PageIndex,
                    RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                    TotalCount = pTotalCount.Value != DBNull.Value && pTotalCount.Value != null ? Convert.ToInt32(pTotalCount.Value) : 0,
                    Qty = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0
                };
            }
            return response;
        }

        public async Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request)
        {
            string connectionString = _configuration.GetConnectionString("POS")!;

            using SqlConnection connection = new SqlConnection(connectionString);

            using SqlCommand command = new SqlCommand("SP_NEW_DASHBOARD", connection);

            command.CommandType = CommandType.StoredProcedure;

            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(request.FromDate))
            {
                fromDate = DateTime.Parse(
                    request.FromDate.Trim('"'));
            }

            if (!string.IsNullOrWhiteSpace(request.ToDate))
            {
                toDate = DateTime.Parse(
                    request.ToDate.Trim('"'));
            }

            // Stored procedure parameters
            command.Parameters.AddWithValue("@status", "LAST7DAY_VOID_DASHBOARD");

            command.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");

            command.Parameters.AddWithValue("@PageIndex", request.PageIndex);

            command.Parameters.AddWithValue("@PageSize", request.PageSize);

            command.Parameters.AddWithValue("@Store_Code", request.StoreName ?? "");

            command.Parameters.Add("@fromdate", SqlDbType.Date).Value =fromDate.HasValue ? fromDate.Value.Date : DBNull.Value;

            command.Parameters.Add("@todate", SqlDbType.Date).Value = toDate.HasValue ? toDate.Value.Date : DBNull.Value;

            command.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn);

            command.Parameters.AddWithValue("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection);


            // Output parameters

            SqlParameter recordCountParameter = new SqlParameter("@RecordCount", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter qtyParameter = new SqlParameter("@QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };

            SqlParameter encodedQtyParameter = new SqlParameter("@ENCODED_QTY", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter differenceQtyParameter =
                new SqlParameter("@DIFF_QTY", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };


            command.Parameters.Add(recordCountParameter);
            command.Parameters.Add(qtyParameter);
            command.Parameters.Add(encodedQtyParameter);
            command.Parameters.Add(differenceQtyParameter);



            DataSet dataSet = new DataSet();

            using SqlDataAdapter adapter = new SqlDataAdapter(command);

            adapter.Fill(dataSet);



            int recordCount = recordCountParameter.Value == DBNull.Value ? 0 : Convert.ToInt32(recordCountParameter.Value);

            int voidCount = qtyParameter.Value == DBNull.Value ? 0 : Convert.ToInt32(qtyParameter.Value);

            int encodeCount = encodedQtyParameter.Value == DBNull.Value ? 0 : Convert.ToInt32(encodedQtyParameter.Value);

            int differenceCount = differenceQtyParameter.Value == DBNull.Value ? 0 : Convert.ToInt32(differenceQtyParameter.Value);


            // Return response

            return new VoidDetailsResponse
            {
                Data = dataSet.Tables[0].AsEnumerable()
                .Select(row => dataSet.Tables[0].Columns.Cast<DataColumn>()
                    .ToDictionary(
                        column => column.ColumnName,
                        column => row[column] == DBNull.Value
                            ? null
                            : row[column]
                    ))
                .ToList(),

                PageIndex = request.PageIndex,

                RecordCount = recordCount,

                VoidQty = voidCount,

                EncodeQty = encodeCount,

                DifferenceQty = differenceCount
            };
        }

        public async Task<VoidReconciliationResponse> GetVoidReconciliationDataAsync(VoidReconciliationRequest request)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("POS")!;

                using SqlConnection con = new SqlConnection(connectionString);

                using SqlCommand cmd = new SqlCommand("SP_NEW_REPORT", con);

                DateTime? fromDate = null;
                DateTime? toDate = null;

                string fromDateValue = request.FromDate?.Trim('"');
                string ToDateValue = request.ToDate?.Trim('"');

                if (!string.IsNullOrWhiteSpace(fromDateValue))
                {
                    fromDate = DateTime.Parse(fromDateValue);
                }

                if (!string.IsNullOrWhiteSpace(ToDateValue))
                {
                    toDate = DateTime.Parse(ToDateValue);
                }

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@status", "SHOW_SUMMARY_FOR_VOID");
                cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");

                cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);

                cmd.Parameters.AddWithValue("@PageSize", request.PageSize);

                cmd.Parameters.Add("@fromdate", SqlDbType.Date).Value = fromDate.HasValue ? fromDate.Value.Date : DBNull.Value;

                cmd.Parameters.Add("@todate", SqlDbType.Date).Value = toDate.HasValue ? toDate.Value.Date : DBNull.Value;

                cmd.Parameters.AddWithValue("@STORE_CODE", request.StoreName ?? "");

                cmd.Parameters.AddWithValue("@COUNTER_NO", request.pos ?? "");

                cmd.Parameters.AddWithValue("@EAN", request.Ean ?? "");

                cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "VOID_DATE" : request.SortColumn);

                cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                // OUTPUT PARAMETERS
                SqlParameter recordCountParam = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                recordCountParam.Direction = ParameterDirection.Output;

                SqlParameter qtyParam = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                qtyParam.Direction = ParameterDirection.Output;

                SqlParameter encQtyParam = cmd.Parameters.Add("@ENCQTY", SqlDbType.Int);
                encQtyParam.Direction = ParameterDirection.Output;

                SqlParameter diffQtyParam = cmd.Parameters.Add("@DIFFQTY", SqlDbType.Int);
                diffQtyParam.Direction = ParameterDirection.Output;

                await con.OpenAsync();

                DataSet ds = new DataSet();

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(ds);
                }

                int recordCount = recordCountParam.Value == DBNull.Value ? 0 : Convert.ToInt32(recordCountParam.Value);

                int voidQty = qtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(qtyParam.Value);

                int encodeQty = encQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(encQtyParam.Value);

                int differenceQty = diffQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(diffQtyParam.Value);


                return new VoidReconciliationResponse
                {
                    PageIndex = request.PageIndex,
                    RecordCount = recordCount,
                    VoidQty = voidQty,
                    EncodeQty = encodeQty,
                    DifferenceQty = differenceQty,

                    Data = ds.Tables[0].AsEnumerable()
                    .Select(row => ds.Tables[0].Columns.Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row[column] == DBNull.Value
                                ? null
                                : row[column]
                        ))
                    .ToList()
                    
                };
            }
            catch(Exception ex)
            {
                return new VoidReconciliationResponse();
            }
           
        }
    }
}

