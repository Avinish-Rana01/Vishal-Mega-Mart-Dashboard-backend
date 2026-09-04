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

namespace VS_Mart_Backend.Features.CycleCountReport
{
    public class CycleCountReportService : BaseDashboardService, ICycleCountReportService
    {
        public CycleCountReportService(IConfiguration configuration, IMemoryCache cache)
            : base(configuration, cache)
        {
        }

        public async Task<CycleCountReportViewResponse> GetCycleCountReportViewAsync(CycleCountReportViewQueryRequest request)
        {
            string cacheKey = $"CycleCountReportView_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.StoreCode}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new CycleCountReportViewResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "CYCLE_COUNT_REPORT_VIEW", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", request.SortDirection ?? "DESC", DbType.String, size: 10);

                    if (!string.IsNullOrEmpty(request.FromDate))
                        parameters.Add("@FromDate", request.FromDate, DbType.String, size: 20);
                    if (!string.IsNullOrEmpty(request.ToDate))
                        parameters.Add("@ToDate", request.ToDate, DbType.String, size: 20);
                    if (!string.IsNullOrEmpty(request.StoreCode))
                        parameters.Add("@Store_code", request.StoreCode, DbType.String, size: 50);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Items = items.Select(x => new Dictionary<string, object?>((IDictionary<string, object>)x, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new CycleCountReportViewSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        RefNo = parameters.Get<int?>("@QTY") ?? 0
                    };

                    return response;
                }
                catch (Exception)
                {
                    return new CycleCountReportViewResponse();
                }
            });
        }

        public async Task<CycleCountDetailsResponse> GetCycleCountDetailsAsync(CycleCountDetailsQueryRequest request)
        {
            string cacheKey = $"CycleCountDetails_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.StoreCode}_{request.FromDate}_{request.ToDate}_{request.RefNo}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new CycleCountDetailsResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "CYCLE_COUNT_REPORT", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_code", request.StoreCode ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.FromDate ?? "", DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate ?? "", DbType.String, size: 20);
                    parameters.Add("@ref_No", request.RefNo ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "STORE_CODE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Ttl_Act_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Sum_Scanned_Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Excess_Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Items = items.Select(x => new Dictionary<string, object?>((IDictionary<string, object>)x, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new CycleCountDetailsSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        TotalCount = parameters.Get<int?>("@Qty") ?? 0,
                        ActualQty = parameters.Get<int?>("@Ttl_Act_QTY") ?? 0,
                        ScannedQty = parameters.Get<int?>("@Sum_Scanned_Qty") ?? 0,
                        DiffQty = parameters.Get<int?>("@DIFFQTY") ?? 0,
                        ExcessQty = parameters.Get<int?>("@Excess_Qty") ?? 0
                    };

                    return response;
                }
                catch (Exception)
                {
                    return new CycleCountDetailsResponse();
                }
            });
        }
    }
}
