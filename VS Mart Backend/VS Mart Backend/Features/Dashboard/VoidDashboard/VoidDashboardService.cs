using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using VS_Mart_Backend.Features.Base;

namespace VS_Mart_Backend.Features.VoidDashboard
{
    public class VoidDashboardService : BaseDashboardService, IVoidDashboardService
    {
        private readonly VS_Mart_Backend.Features.MainDashboard.IMainDashboardService _mainDashboardService;

        public VoidDashboardService(
            IConfiguration configuration,
            IMemoryCache cache,
            VS_Mart_Backend.Features.MainDashboard.IMainDashboardService mainDashboardService)
            : base(configuration, cache)
        {
            _mainDashboardService = mainDashboardService;
        }

        public async Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request)
        {
            var mainRequest = new Features.MainDashboard.VoidDashboardQueryRequest
            {
                UserId = request.UserId,
                SearchTerm = request.SearchTerm,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                SortColumn = request.SortColumn,
                SortDirection = request.SortDirection,
                SortType = request.SortType
            };

            var mainResponse = await _mainDashboardService.GetVoidDashboardAsync(mainRequest);

            return new VoidDashboardResponse
            {
                Summary = new VoidDashboardSummary
                {
                    RecordCount = mainResponse.Summary.RecordCount,
                    TotalCount = mainResponse.Summary.TotalCount,
                    ReturnQty = mainResponse.Summary.ReturnQty,
                    ReturnEncodedQty = mainResponse.Summary.ReturnEncodedQty,
                    PendingQty = mainResponse.Summary.PendingQty
                },
                Items = mainResponse.Items
            };
        }

        public async Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request)
        {
            string cacheKey = $"VoidDetails_{request.StoreName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new VoidDetailsResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "LAST7DAY_VOID_DASHBOARD", DbType.String, size: 50);
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
                    response.VoidQty = parameters.Get<int?>("@QTY") ?? 0;
                    response.EncodeQty = parameters.Get<int?>("@ENCODED_QTY") ?? 0;
                    response.DifferenceQty = parameters.Get<int?>("@DIFF_QTY") ?? 0;

                    return response;
                }
                catch (Exception)
                {
                    return new VoidDetailsResponse();
                }
            });
        }

        public async Task<VoidReconciliationResponse> GetVoidReconciliationDataAsync(VoidReconciliationRequest request)
        {
            string cacheKey = $"VoidReconciliation_{request.StoreName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.FromDate}_{request.ToDate}_{request.pos}_{request.Ean}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new VoidReconciliationResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "SHOW_SUMMARY_FOR_VOID", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@fromdate", fromDate.HasValue ? fromDate.Value.Date : null, DbType.Date);
                    parameters.Add("@todate", toDate.HasValue ? toDate.Value.Date : null, DbType.Date);
                    parameters.Add("@STORE_CODE", request.StoreName ?? "", DbType.String, size: 50);
                    parameters.Add("@COUNTER_NO", request.pos ?? "", DbType.String, size: 50);
                    parameters.Add("@EAN", request.Ean ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "VOID_DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", request.SortDirection ?? "asc", DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.VoidQty = parameters.Get<int?>("@QTY") ?? 0;
                    response.EncodeQty = parameters.Get<int?>("@ENCQTY") ?? 0;
                    response.DifferenceQty = parameters.Get<int?>("@DIFFQTY") ?? 0;

                    return response;
                }
                catch (Exception)
                {
                    return new VoidReconciliationResponse();
                }
            });
        }

        public async Task<List<POSCounterResponse>> VoidBindPOSCounter(BindPOSCounterRequest request)
        {
            string cacheKey = $"VoidBindPOSCounter_{request.Store}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", "BIND_COUNTER_FOR_VOID", DbType.String, size: 50);
                    parameters.Add("@fromDate", fromDate, DbType.DateTime);
                    parameters.Add("@todate", toDate, DbType.DateTime);
                    parameters.Add("@STORE_CODE", request.Store ?? "", DbType.String, size: 50);

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

        public async Task<List<EANItem>> SearchEAN(SearchEANRequest request)
        {
            string cacheKey = $"SearchEAN_{request.SearchTerm}_{request.Store}_{request.Pos}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = string.IsNullOrEmpty(request.SearchTerm) ? "BIND_EAN_FOR_VOID" : "SEARCH_BIND_EAN_FOR_VOID";

                    DateTime? fromDate = !string.IsNullOrWhiteSpace(request.FromDate) ? DateTime.Parse(request.FromDate.Trim('"')) : null;
                    DateTime? toDate = !string.IsNullOrWhiteSpace(request.ToDate) ? DateTime.Parse(request.ToDate.Trim('"')) : null;

                    parameters.Add("@status", status, DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@STORE_CODE", request.Store ?? "", DbType.String, size: 50);
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

        public async Task<VoidReconciliationModelResponse> GetVoidReconciliationDataModelAsync(VoidReconciliationModelRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // =========================================
                // Dapper Parameters
                // =========================================

                var parameters = new DynamicParameters();

                parameters.Add("@status", "SHOW_SUMMARY_DATA_FOR_VOID", DbType.String);

                parameters.Add("@SearchTerm", (request.SearchTerm ?? "").Trim(), DbType.String);

                parameters.Add("@PageIndex", request.PageIndex <= 0 ? 1 : request.PageIndex, DbType.Int32);

                parameters.Add("@PageSize", request.PageSize <= 0 ? 10 : request.PageSize, DbType.Int32);

                string cleanDate = (request.BillDate ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanDate))
                {
                    if (cleanDate.Contains('T')) cleanDate = cleanDate.Split('T')[0].Trim();
                    string[] formats = { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy/MM/dd", "MM/dd/yyyy", "MM-dd-yyyy" };
                    if (DateTime.TryParseExact(cleanDate, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDt) ||
                        DateTime.TryParse(cleanDate, out parsedDt))
                    {
                        cleanDate = parsedDt.ToString("yyyy-MM-dd");
                    }
                }
                parameters.Add("@BILL_DATE", cleanDate, DbType.String);

                string cleanStore = (request.StoreCode ?? "").Trim();
                if (cleanStore.Contains('-'))
                {
                    cleanStore = cleanStore.Split('-')[0].Trim();
                }
                parameters.Add("@STORE_CODE", cleanStore, DbType.String);

                string cleanPos = (request.Pos ?? "").Trim();
                parameters.Add("@COUNTER_NO", cleanPos, DbType.String);

                string cleanEan = (request.Ean ?? "").Trim();
                parameters.Add("@EAN", cleanEan, DbType.String);

                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "VOID_DATE" : request.SortColumn.Trim(), DbType.String);

                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "ASC" : request.SortDirection.Trim(), DbType.String);


                // =========================================
                // Output Parameters
                // =========================================

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // =========================================
                // Execute Stored Procedure
                // =========================================

                var data = await connection.QueryAsync<VoidReconciliationModel>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);


                // =========================================
                // Get Output Values
                // =========================================

                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;

                int voidQty = parameters.Get<int?>("@QTY") ?? 0;

                int encodeQty = parameters.Get<int?>("@ENCQTY") ?? 0;

                int differenceQty = parameters.Get<int?>("@DIFFQTY") ?? 0;


                // =========================================
                // Response
                // =========================================

                return new VoidReconciliationModelResponse
                {
                    PageIndex = request.PageIndex,

                    RecordCount = recordCount,

                    VoidQty = voidQty,

                    EncodeQty = encodeQty,

                    DifferenceQty = differenceQty,

                    Data = data.ToList()
                };
            }
            catch (Exception)
            {
                return new VoidReconciliationModelResponse();
            }
        }
    }
}
