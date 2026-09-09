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

namespace VS_Mart_Backend.Features.ReturnDashboard
{
    public interface IReturnDashboardService
    {
        Task<ReturnDetailsResponse> GetReturnDetailsAsync(ReturnDetailsRequest request);
        Task<ReturnReconciliationResponse> GetReturnReconciliationData(ReturnReconciliationRequest request);
    }

    public class ReturnDashboardService : BaseDashboardService, IReturnDashboardService
    {
        public ReturnDashboardService(IConfiguration configuration, IMemoryCache cache)
            : base(configuration, cache)
        {
        }

        public async Task<ReturnDetailsResponse> GetReturnDetailsAsync(ReturnDetailsRequest request)
        {
            string cacheKey = $"ReturnDetails_{request.StoreName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new ReturnDetailsResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "LAST7DAY_RETURN_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_Code", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.ReturnQty = parameters.Get<int?>("@QTY") ?? 0;
                    response.EncodeQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0;
                    response.DifferenceQty = parameters.Get<int?>("@DIFF_QTY") ?? 0;

                    return response;
                }
                catch (Exception)
                {
                    return new ReturnDetailsResponse();
                }
            });
        }

        public async Task<ReturnReconciliationResponse> GetReturnReconciliationData(ReturnReconciliationRequest request)
        {
            string cacheKey = $"ReturnReconciliation_{request.StoreName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.Pos}_{request.Ean}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new ReturnReconciliationResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "SHOW_SUMMARY_FOR_RETURN", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@STORE_CODE", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@COUNTER_NO", request.Pos ?? "", DbType.String, size: 50);
                    parameters.Add("@EAN", request.Ean ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "BILL_DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.ReturnQty = parameters.Get<int?>("@QTY") ?? 0;
                    response.EncodeQty = parameters.Get<int?>("@ENCQTY") ?? 0;
                    response.DifferenceQty = parameters.Get<int?>("@DIFFQTY") ?? 0;

                    return response;
                }
                catch (Exception)
                {
                    return new ReturnReconciliationResponse();
                }
            });
        }
    }
}
