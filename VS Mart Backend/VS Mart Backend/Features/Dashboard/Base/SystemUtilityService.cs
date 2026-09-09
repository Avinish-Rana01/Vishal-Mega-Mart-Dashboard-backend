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

namespace VS_Mart_Backend.Features.SystemUtility
{
    public interface ISystemUtilityService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);
        Task<EncodingStoreDataResponse> GetEncodingStoreDataAsync(EncodingStoreDataRequest request);
    }

    public class SystemUtilityService : BaseDashboardService, ISystemUtilityService
    {
        public SystemUtilityService(IConfiguration configuration, IMemoryCache cache)
            : base(configuration, cache)
        {
        }

        public async Task<EncodingStoreDataResponse> GetEncodingStoreDataAsync(EncodingStoreDataRequest request)
        {
            string cacheKey = $"EncodingStoreData_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.Ean}_{request.ArticleNo}_{request.StoreName}_{request.UserId}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new EncodingStoreDataResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "ENCODING_SHOW_DATA_FOR_STORE", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@EAN", request.Ean ?? "", DbType.String, size: 50);
                    parameters.Add("@Material", request.ArticleNo ?? "", DbType.String, size: 50);
                    parameters.Add("@Store_Code", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "ARTICLE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "asc" : request.SortDirection, DbType.String, size: 10);
                    parameters.Add("@User_ID", request.UserId ?? 0, DbType.Int32);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.TotalCount = parameters.Get<int?>("@TotalCount") ?? 0;

                    return response;
                }
                catch (Exception)
                {
                    return new EncodingStoreDataResponse();
                }
            });
        }
    }
}
