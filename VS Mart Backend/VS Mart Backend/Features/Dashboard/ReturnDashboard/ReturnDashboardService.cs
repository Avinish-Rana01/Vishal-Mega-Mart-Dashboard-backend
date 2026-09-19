using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;
using VS_Mart_Backend.Features.VoidDashboard;

namespace VS_Mart_Backend.Features.ReturnDashboard
{
    public interface IReturnDashboardService
    {
        Task<ReturnDetailsResponse> GetReturnDetailsAsync(ReturnDetailsRequest request);
        Task<ReturnReconciliationResponse> GetReturnReconciliationData(ReturnReconciliationRequest request);
        Task<List<POSCounterResponse>> ReturnBindPOSCounter(BindPOSCounterRequest request);
        Task<List<EANItem>> ReturnSearchEAN(SearchEANRequest request);
        Task<ReturnReconciliationModelResponse> GetReturnReconciliationDataModelAsync(ReturnReconciliationModelRequest request);
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

                    string cleanStore = (request.StoreName ?? "").Trim();
                    if (cleanStore.Contains('-')) cleanStore = cleanStore.Split('-')[0].Trim();

                    DateTime? fromDate = null;
                    if (!string.IsNullOrWhiteSpace(request.FromDate))
                    {
                        string dStr = request.FromDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) fromDate = d;
                    }

                    DateTime? toDate = null;
                    if (!string.IsNullOrWhiteSpace(request.ToDate))
                    {
                        string dStr = request.ToDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) toDate = d;
                    }

                    parameters.Add("@status", "LAST7DAY_RETURN_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_Code", cleanStore, DbType.String, size: 50);
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

                    string cleanStore = (request.StoreName ?? "").Trim();
                    if (cleanStore.Contains('-')) cleanStore = cleanStore.Split('-')[0].Trim();

                    DateTime? fromDate = null;
                    if (!string.IsNullOrWhiteSpace(request.FromDate))
                    {
                        string dStr = request.FromDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) fromDate = d;
                    }

                    DateTime? toDate = null;
                    if (!string.IsNullOrWhiteSpace(request.ToDate))
                    {
                        string dStr = request.ToDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) toDate = d;
                    }

                    parameters.Add("@status", "SHOW_SUMMARY_FOR_RETURN", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@STORE_CODE", cleanStore, DbType.String, size: 50);
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

        public async Task<List<POSCounterResponse>> ReturnBindPOSCounter(BindPOSCounterRequest request)
        {
            string cacheKey = $"ReturnBindPOSCounter_{request.Store}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string cleanStore = (request.Store ?? "").Trim();
                    if (cleanStore.Contains('-')) cleanStore = cleanStore.Split('-')[0].Trim();

                    DateTime? fromDate = null;
                    if (!string.IsNullOrWhiteSpace(request.FromDate))
                    {
                        string dStr = request.FromDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) fromDate = d;
                    }

                    DateTime? toDate = null;
                    if (!string.IsNullOrWhiteSpace(request.ToDate))
                    {
                        string dStr = request.ToDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) toDate = d;
                    }

                    parameters.Add("@status", "BIND_COUNTER_FOR_RETURN", DbType.String, size: 50);
                    parameters.Add("@fromDate", fromDate, DbType.DateTime);
                    parameters.Add("@todate", toDate, DbType.DateTime);
                    parameters.Add("@STORE_CODE", cleanStore, DbType.String, size: 50);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("COUNTER_NO") && x["COUNTER_NO"] != null)
                        .Select(x => new POSCounterResponse { id = x["COUNTER_NO"].ToString()!, text = x["COUNTER_NO"].ToString()! })
                        .ToList();
                }
                catch (Exception)
                {
                    return new List<POSCounterResponse>();
                }
            });
        }

        public async Task<List<EANItem>> ReturnSearchEAN(SearchEANRequest request)
        {
            string cacheKey = $"ReturnSearchEAN_{request.SearchTerm}_{request.Store}_{request.Pos}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = string.IsNullOrEmpty(request.SearchTerm) ? "BIND_EAN_FOR_RETURN" : "SEARCH_BIND_EAN_FOR_RETURN";

                    string cleanStore = (request.Store ?? "").Trim();
                    if (cleanStore.Contains('-')) cleanStore = cleanStore.Split('-')[0].Trim();

                    DateTime? fromDate = null;
                    if (!string.IsNullOrWhiteSpace(request.FromDate))
                    {
                        string dStr = request.FromDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) fromDate = d;
                    }

                    DateTime? toDate = null;
                    if (!string.IsNullOrWhiteSpace(request.ToDate))
                    {
                        string dStr = request.ToDate.Trim().Trim('"');
                        if (dStr.Contains('T')) dStr = dStr.Split('T')[0].Trim();
                        if (DateTime.TryParse(dStr, out var d)) toDate = d;
                    }

                    parameters.Add("@status", status, DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@STORE_CODE", cleanStore, DbType.String, size: 50);
                    parameters.Add("@fromDate", fromDate, DbType.DateTime);
                    parameters.Add("@toDate", toDate, DbType.DateTime);
                    parameters.Add("@COUNTER_NO", request.Pos ?? "", DbType.String, size: 50);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("EAN") && x["EAN"] != null)
                        .Select(x => new EANItem { id = x["EAN"].ToString()!, text = x["EAN"].ToString()! })
                        .ToList();
                }
                catch (Exception)
                {
                    return new List<EANItem>();
                }
            });
        }

        public async Task<ReturnReconciliationModelResponse> GetReturnReconciliationDataModelAsync(ReturnReconciliationModelRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@status", "SHOW_SUMMARY_DATA_FOR_RETURN", DbType.String);
                parameters.Add("@SearchTerm", (request.SearchTerm ?? "").Trim(), DbType.String);
                parameters.Add("@PageIndex", request.PageIndex <= 0 ? 1 : request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize <= 0 ? 10 : request.PageSize, DbType.Int32);

                string cleanDate = (request.BillDate ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanDate))
                {
                    if (cleanDate.Contains('T')) cleanDate = cleanDate.Split('T')[0].Trim();
                    string[] formats = { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy/MM/dd", "MM/dd/yyyy", "MM-dd-yyyy" };
                    if (DateTime.TryParseExact(cleanDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt) ||
                        DateTime.TryParse(cleanDate, out parsedDt))
                    {
                        cleanDate = parsedDt.ToString("yyyy-MM-dd");
                    }
                }
                parameters.Add("@BILL_DATE", cleanDate, DbType.String);

                string cleanStore = (request.StoreCode ?? "").Trim();
                if (cleanStore.Contains('-')) cleanStore = cleanStore.Split('-')[0].Trim();
                parameters.Add("@STORE_CODE", cleanStore, DbType.String);

                parameters.Add("@COUNTER_NO", (request.Pos ?? "").Trim(), DbType.String);
                parameters.Add("@EAN", (request.Ean ?? "").Trim(), DbType.String);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "BILL_DATE" : request.SortColumn.Trim(), DbType.String);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "ASC" : request.SortDirection.Trim(), DbType.String);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var data = await connection.QueryAsync<ReturnReconciliationModel>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                int returnQty = parameters.Get<int?>("@QTY") ?? 0;
                int encodeQty = parameters.Get<int?>("@ENCQTY") ?? 0;
                int diffQty = parameters.Get<int?>("@DIFFQTY") ?? 0;

                return new ReturnReconciliationModelResponse
                {
                    PageIndex = request.PageIndex,
                    RecordCount = recordCount,
                    ReturnQty = returnQty,
                    EncodeQty = encodeQty,
                    DifferenceQty = diffQty,
                    Data = data
                };
            }
            catch (Exception)
            {
                return new ReturnReconciliationModelResponse();
            }
        }
    }
}
