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

namespace VS_Mart_Backend.Features.DcDashboard
{
    public interface IDcDashboardService
    {
        Task<DCDetailsResponse> GetDCDetailsAsync(DCDetailsRequest request);
        Task<HUDetailsResponse> GetHUDetailsAsync(HUDetailsRequest request);
        Task<List<HuNumberItem>> SearchValidationHuNumbersAsync(string? huStatus, string? receivingPlant, string? fromDate, string? toDate, string? searchTerm);
    }

    public class DcDashboardService : BaseDashboardService, IDcDashboardService
    {
        private readonly ILogger<DcDashboardService> _logger;

        public DcDashboardService(IConfiguration configuration, IMemoryCache cache, ILogger<DcDashboardService> logger)
            : base(configuration, cache)
        {
            _logger = logger;
        }

        public async Task<DCDetailsResponse> GetDCDetailsAsync(DCDetailsRequest request)
        {
            string cacheKey = $"DCDetails_{request.StoreName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new DCDetailsResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "LAST7DAY_DC_VALIDATE_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_Code", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@PROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@UNPROCESSED_HU", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@PROCESSED_ARTICLE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.ProcessedCount = parameters.Get<int?>("@PROCESSED_HU") ?? 0;
                    response.UnprocessedCount = parameters.Get<int?>("@UNPROCESSED_HU") ?? 0;
                    response.ValidatedCount = parameters.Get<int?>("@PROCESSED_ARTICLE_QTY") ?? 0;

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching DC details");
                    return new DCDetailsResponse();
                }
            });
        }

        public async Task<HUDetailsResponse> GetHUDetailsAsync(HUDetailsRequest request)
        {
            try
            {
                var response = new HUDetailsResponse();
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                DateTime? parsedFrom = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                DateTime? parsedTo = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                int pageIndex = request.PageIndex <= 0 ? 1 : request.PageIndex;
                int pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

                parameters.Add("@status", "HU_REPORT", DbType.String, size: 50);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                parameters.Add("@CI_STATUS", string.IsNullOrEmpty(request.HUStatus) ? "1" : request.HUStatus, DbType.String, size: 50);
                parameters.Add("@HU_NO", request.HUNo ?? "", DbType.String, size: 50);
                parameters.Add("@fromdate", parsedFrom.HasValue ? parsedFrom.Value.Date : null, DbType.Date);
                parameters.Add("@todate", parsedTo.HasValue ? parsedTo.Value.Date : null, DbType.Date);
                parameters.Add("@Reciving_Plant", request.ReceivingPlant ?? "", DbType.String, size: 50);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "HU_Number" : request.SortColumn, DbType.String, size: 50);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection, DbType.String, size: 10);

                parameters.Add("@HUCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@MATERIALCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@SCANQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TAGQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                response.PageIndex = pageIndex;
                response.RecordCount = parameters.Get<int?>("@RecordCount") ?? parameters.Get<int?>("@HUCOUNT") ?? 0;
                response.MaterialQty = parameters.Get<int?>("@MATERIALCOUNT") ?? 0;
                response.ActualQty = parameters.Get<int?>("@ACTUALQTY") ?? 0;
                response.ScannedQty = parameters.Get<int?>("@SCANQTY") ?? 0;
                response.InvalidTags = parameters.Get<int?>("@TAGQTY") ?? 0;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching HU details");
                return new HUDetailsResponse();
            }
        }

        public async Task<List<HuNumberItem>> SearchValidationHuNumbersAsync(string? huStatus, string? receivingPlant, string? fromDate, string? toDate, string? searchTerm)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                bool isSearch = !string.IsNullOrEmpty(searchTerm);
                string status = isSearch ? "SEARCH_BIND_HU_NUMBER" : "BIND_HU_NUMBER";

                parameters.Add("@status", status, DbType.String, size: 50);
                parameters.Add("@CI_STATUS", string.IsNullOrEmpty(huStatus) ? "1" : huStatus, DbType.String, size: 50);
                parameters.Add("@Reciving_Plant", receivingPlant ?? "", DbType.String, size: 50);

                DateTime? parsedFrom = !string.IsNullOrWhiteSpace(fromDate) ? DateTime.Parse(fromDate.Trim('"')) : null;
                DateTime? parsedTo = !string.IsNullOrWhiteSpace(toDate) ? DateTime.Parse(toDate.Trim('"')) : null;

                parameters.Add("@fromdate", parsedFrom.HasValue ? parsedFrom.Value.Date : null, DbType.Date);
                parameters.Add("@todate", parsedTo.HasValue ? parsedTo.Value.Date : null, DbType.Date);

                if (isSearch)
                {
                    parameters.Add("@SearchTerm", searchTerm ?? "", DbType.String, size: 200);
                }

                var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                return items
                    .Select(x => (IDictionary<string, object>)x)
                    .Where(x => x.ContainsKey("HU_NUMBER") && x["HU_NUMBER"] != null)
                    .Select(x => new HuNumberItem { Id = x["HU_NUMBER"].ToString()!, Value = x["HU_NUMBER"].ToString()!, Text = x["HU_NUMBER"].ToString()! })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching validation HU numbers");
                return new List<HuNumberItem>();
            }
        }
    }
}
