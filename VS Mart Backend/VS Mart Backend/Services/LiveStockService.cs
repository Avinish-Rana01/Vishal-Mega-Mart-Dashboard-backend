using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using VS_Mart_Backend.Models;

namespace VS_Mart_Backend.Services
{
    public interface ILiveStockService
    {
        Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request);
        Task<LiveStockResponse> GetLiveStockReportAsync(LiveStockReportQueryRequest request);
        Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request);
        Task<CycleCountReportViewResponse> GetCycleCountReportViewAsync(CycleCountReportViewQueryRequest request);
        Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request);
    }

    public class LiveStockService : ILiveStockService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public LiveStockService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request)
        {
            string cacheKey = $"LiveStockDetails_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}";

            if (_cache.TryGetValue(cacheKey, out LiveStockResponse? cachedResponse) && cachedResponse != null)
            {
                return cachedResponse;
            }

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

                    // Extract Output Parameters after reader completes
                    response.Summary = new LiveStockSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                        TotalCount = pTotalCount.Value != DBNull.Value && pTotalCount.Value != null ? Convert.ToInt32(pTotalCount.Value) : 0,
                        SapQty = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0,
                        RfidQty = pEncodedQty.Value != DBNull.Value && pEncodedQty.Value != null ? Convert.ToInt32(pEncodedQty.Value) : 0,
                        DiffQty = pDiffQty.Value != DBNull.Value && pDiffQty.Value != null ? Convert.ToInt32(pDiffQty.Value) : 0
                    };
                }
            }

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
        }

        public async Task<LiveStockResponse> GetLiveStockReportAsync(LiveStockReportQueryRequest request)
        {
            string cacheKey = $"LiveStockReport_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            if (_cache.TryGetValue(cacheKey, out LiveStockResponse? cachedResponse) && cachedResponse != null)
            {
                return cachedResponse;
            }

            var response = new LiveStockResponse();
            string connectionString = _configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand("[SP_New_Dashboard]", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input Parameters
                    cmd.Parameters.AddWithValue("@status", "LIVE_STOCK_REPORT");
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn);
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                    if (!string.IsNullOrEmpty(request.FromDate))
                        cmd.Parameters.AddWithValue("@FromDate", request.FromDate);
                    if (!string.IsNullOrEmpty(request.ToDate))
                        cmd.Parameters.AddWithValue("@ToDate", request.ToDate);
                    if (!string.IsNullOrEmpty(request.ArticleNo))
                        cmd.Parameters.AddWithValue("@ArticleNo", request.ArticleNo);
                    if (!string.IsNullOrEmpty(request.StoreName))
                        cmd.Parameters.AddWithValue("@StoreName", request.StoreName);

                    // Output Parameters
                    var pRecordCount = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                    pRecordCount.Direction = ParameterDirection.Output;

                    var pQty = cmd.Parameters.Add("@QTY", SqlDbType.Int);
                    pQty.Direction = ParameterDirection.Output;

                    var pEncQty = cmd.Parameters.Add("@ENCODED_QTY", SqlDbType.Int);
                    pEncQty.Direction = ParameterDirection.Output;

                    var pDiffQty = cmd.Parameters.Add("@DIFF_QTY", SqlDbType.Int);
                    pDiffQty.Direction = ParameterDirection.Output;

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

                    response.Summary = new LiveStockSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                        TotalCount = pRecordCount.Value != DBNull.Value && pRecordCount.Value != null ? Convert.ToInt32(pRecordCount.Value) : 0,
                        SapQty = pQty.Value != DBNull.Value && pQty.Value != null ? Convert.ToInt32(pQty.Value) : 0,
                        RfidQty = pEncQty.Value != DBNull.Value && pEncQty.Value != null ? Convert.ToInt32(pEncQty.Value) : 0,
                        DiffQty = pDiffQty.Value != DBNull.Value && pDiffQty.Value != null ? Convert.ToInt32(pDiffQty.Value) : 0
                    };
                }
            }

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
        }

        public async Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request)
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

            if (_cache.TryGetValue(cacheKey, out CycleCountDashboardResponse? cachedResponse) && cachedResponse != null)
            {
                return cachedResponse;
            }

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

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
        }
    }
}
