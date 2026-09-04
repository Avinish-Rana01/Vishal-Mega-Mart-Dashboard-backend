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

namespace VS_Mart_Backend.Features.LiveStockReport
{
    public class LiveStockReportService : BaseDashboardService, ILiveStockReportService
    {
        private readonly ILogger<LiveStockReportService> _logger;

        public LiveStockReportService(IConfiguration configuration, IMemoryCache cache, ILogger<LiveStockReportService> logger)
            : base(configuration, cache)
        {
            _logger = logger;
        }

        public async Task<List<StoreDropdownItem>> GetStoresAsync(string? userId)
        {
            string cacheKey = $"GetStores_LiveStock_{userId}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "BIND_STORE_FOR_ALL", DbType.String, size: 50);
                    parameters.Add("@USER_ID", string.IsNullOrEmpty(userId) ? null : userId, DbType.String);
                    parameters.Add("@FromDate", "", DbType.String);
                    parameters.Add("@ToDate", "", DbType.String);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("STORE_CODE") && x["STORE_CODE"] != null)
                        .Select(x => new StoreDropdownItem { Value = x["STORE_CODE"].ToString()!, Text = x["STORE_CODE"].ToString()! })
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching stores");
                    return new List<StoreDropdownItem>();
                }
            });
        }

        public async Task<List<ArticleItem>> SearchArticlesAsync(ArticleSearchRequest request)
        {
            string cacheKey = $"SearchArticles_LiveStock_{request.SearchTerm}_{request.StoreCode}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = string.IsNullOrEmpty(request.SearchTerm) ? "BIND_MATERIAL_FOR_LIVE_STOCK" : "SEARCH_BIND_MATERIAL_FOR_LIVE_STOCK";

                    parameters.Add("@status", status, DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@Store_Code", request.StoreCode ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.FromDate ?? "", DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate ?? "", DbType.String, size: 20);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("MATERIAL") && x["MATERIAL"] != null)
                        .Select(x => new ArticleItem { Id = x["MATERIAL"].ToString()!, Text = x["MATERIAL"].ToString()! })
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching articles");
                    return new List<ArticleItem>();
                }
            });
        }

        public async Task<LiveStockReportResponse> GetLiveStockDetailsAsync(LiveStockReportRequest request)
        {
            string cacheKey = $"LiveStockReportDetails_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.StoreName}_{request.StockDate}_{request.ArticleNo}_{request.SortColumn}_{request.SortDirection}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new LiveStockReportResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "LIVE_STOCK_REPORT", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_Code", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.StockDate ?? "", DbType.String, size: 20);
                    parameters.Add("@todate", request.StockDate ?? "", DbType.String, size: 20);
                    parameters.Add("@Material", request.ArticleNo ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STOCK_DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection, DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => new Dictionary<string, object?>((IDictionary<string, object>)x, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary.PageIndex = request.PageIndex;
                    response.Summary.TotalRecords = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.Summary.SapStockCount = parameters.Get<int?>("@QTY") ?? 0;
                    response.Summary.RfidStockCount = parameters.Get<int?>("@ENCQTY") ?? 0;
                    response.Summary.DifferenceCount = parameters.Get<int?>("@DIFFQTY") ?? 0;
                    response.Summary.StoreName = response.Data.Count > 0 && response.Data[0].ContainsKey("STORE_NAME") ? response.Data[0]["STORE_NAME"]?.ToString() : request.StoreName;
                    response.Summary.Date = request.StockDate;

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching live stock report");
                    return new LiveStockReportResponse();
                }
            });
        }
    }
}
