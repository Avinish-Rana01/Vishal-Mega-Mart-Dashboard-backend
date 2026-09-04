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

namespace VS_Mart_Backend.Features.SaleDashboard
{
    public class SaleDashboardService : BaseDashboardService, ISaleDashboardService
    {
        public SaleDashboardService(IConfiguration configuration, IMemoryCache cache)
            : base(configuration, cache)
        {
        }

        public async Task<StoreSaleReportResponse> GetStoreSaleReportAsync(StoreSaleReportQueryRequest request)
        {
            string cacheKey = $"StoreSaleReport_{request.StoreCode}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new StoreSaleReportResponse();

                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();
                    
                    parameters.Add("@status", "LAST7DAY_SALE_DASHBOARD", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@Store_Code", request.StoreCode ?? "", DbType.String, size: 50);
                    parameters.Add("@fromdate", request.FromDate ?? "", DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate ?? "", DbType.String, size: 20);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DPOS_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@RFID_CHECKOUT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@TAFFETA_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@MANUAL_SALE", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Items = items.Select(x => new Dictionary<string, object?>((IDictionary<string, object>)x, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new StoreSaleReportSummary
                    {
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        POSSaleQty = parameters.Get<int?>("@DPOS_SALE") ?? 0,
                        RFIDCheckoutQty = parameters.Get<int?>("@RFID_CHECKOUT") ?? 0,
                        TaffetaSaleQty = parameters.Get<int?>("@TAFFETA_SALE") ?? 0,
                        ManualSaleQty = parameters.Get<int?>("@MANUAL_SALE") ?? 0,
                        StoreCode = request.StoreCode ?? "",
                        FromDate = request.FromDate ?? "",
                        ToDate = request.ToDate ?? ""
                    };

                    return response;
                }
                catch (Exception)
                {
                    return new StoreSaleReportResponse();
                }
            });
        }

        public async Task<List<DropdownItem>> BindPOSCounterAsync(BindPOSCounterRequest request)
        {
            string cacheKey = $"BindPOSCounter_{request.ColumnName}_{request.Store}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();
                    
                    string status = "";
                    if (request.ColumnName == "TOTAL_DPOS_SALE") status = "BIND_COUNTER_FOR_POS_SALE";
                    else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE") status = "BIND_COUNTER_FOR_RFID_DPOS_SALE";
                    else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID") status = "BIND_COUNTER_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                    if (!string.IsNullOrEmpty(status)) parameters.Add("@status", status);

                    parameters.Add("@fromdate", request.FromDate ?? "");
                    parameters.Add("@todate", string.IsNullOrEmpty(request.ToDate) ? request.FromDate : request.ToDate);
                    parameters.Add("@store_code", request.Store ?? "");

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                    
                    var list = items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("COUNTER_NO") && x["COUNTER_NO"] != null)
                        .Select(x => new DropdownItem { Id = x["COUNTER_NO"].ToString()!, Text = x["COUNTER_NO"].ToString()! })
                        .ToList();

                    return list;
                }
                catch (Exception)
                {
                    return new List<DropdownItem>();
                }
            });
        }

        public async Task<List<DropdownItem>> SearchArticlesSaleAsync(SearchArticlesSaleRequest request)
        {
            string cacheKey = $"SearchArticlesSale_{request.ColumnName}_{request.SearchTerm}_{request.Store}_{request.Pos}_{request.FromDate}_{request.ToDate}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = "";
                    bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                    if (request.ColumnName == "TOTAL_DPOS_SALE")
                        status = isSearch ? "SEARCH_BIND_ARTICLE_FOR_POS_SALE" : "BIND_ARTICLE_FOR_POS_SALE";
                    else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE")
                        status = isSearch ? "SEARCH_BIND_ARTICLE_FOR_RFID_DPOS_SALE" : "BIND_ARTICLE_FOR_RFID_DPOS_SALE";
                    else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID")
                        status = isSearch ? "SEARCH_BIND_MATERIAL_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" : "BIND_MATERIAL_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                    if (!string.IsNullOrEmpty(status)) parameters.Add("@status", status);

                    parameters.Add("@SearchTerm", request.SearchTerm ?? "");
                    parameters.Add("@store_code", request.Store ?? "");
                    parameters.Add("@COUNTER_NO", request.Pos ?? "");
                    parameters.Add("@fromdate", request.FromDate ?? "");
                    parameters.Add("@todate", string.IsNullOrEmpty(request.ToDate) ? request.FromDate : request.ToDate);
                    parameters.Add("@User_ID", "0");

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("ARTICLE") && x["ARTICLE"] != null)
                        .Select(x => new DropdownItem { Id = x["ARTICLE"].ToString()!, Text = x["ARTICLE"].ToString()! })
                        .ToList();
                }
                catch (Exception)
                {
                    return new List<DropdownItem>();
                }
            });
        }

        public async Task<List<DropdownItem>> SearchEANSaleAsync(SearchEANSaleRequest request)
        {
            string cacheKey = $"SearchEANSale_{request.ColumnName}_{request.SearchTerm}_{request.Store}_{request.Pos}_{request.FromDate}_{request.ToDate}_{request.Material}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();
                    
                    string status = "";
                    bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                    if (request.ColumnName == "TOTAL_DPOS_SALE")
                        status = isSearch ? "SEARCH_BIND_EAN_FOR_POS_SALE" : "BIND_EAN_FOR_POS_SALE";
                    else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE")
                        status = isSearch ? "SEARCH_BIND_EAN_FOR_RFID_DPOS_SALE" : "BIND_EAN_FOR_RFID_DPOS_SALE";
                    else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID")
                        status = isSearch ? "SEARCH_BIND_EAN_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" : "BIND_EAN_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                    if (!string.IsNullOrEmpty(status)) parameters.Add("@status", status);

                    parameters.Add("@SearchTerm", request.SearchTerm ?? "");
                    parameters.Add("@store_code", request.Store ?? "");
                    parameters.Add("@COUNTER_NO", request.Pos ?? "");
                    parameters.Add("@fromdate", request.FromDate ?? "");
                    parameters.Add("@todate", request.ToDate ?? "");
                    parameters.Add("@Material", request.Material ?? "");
                    parameters.Add("@User_ID", "0");

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    return items
                        .Select(x => (IDictionary<string, object>)x)
                        .Where(x => x.ContainsKey("EAN") && x["EAN"] != null)
                        .Select(x => new DropdownItem { Id = x["EAN"].ToString()!, Text = x["EAN"].ToString()! })
                        .ToList();
                }
                catch (Exception)
                {
                    return new List<DropdownItem>();
                }
            });
        }

        public async Task<SaleDataResponse> GetSaleDataAsync(SaleDataQueryRequest request)
        {
            string cacheKey = $"SaleData_{request.ColumnName}_{request.SearchTerm}_{request.PageIndex}_{request.PageSize}_{request.StoreName}_{request.FromDate}_{request.ToDate}_{request.Pos}_{request.ArticleNo}_{request.Ean}_{request.UserId}_{request.SortColumn}_{request.SortDirection}";
            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new SaleDataResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    string status = "";
                    if (request.ColumnName == "TOTAL_DPOS_SALE") status = "SHOW_POS_SALE_DATA";
                    else if (request.ColumnName == "TOTAL_RFID_CHECKOUT") status = "SHOW_RFID_CHECKOUT_DATA";
                    else if (request.ColumnName == "TOTAL_RFID_DPOS_SALE") status = "SHOW_RFID_DPOS_SALE_DATA";
                    else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_SALE") status = "SHOW_RFID_CHECKOUT_MATCHING_WITH_DPOS_SALE_DATA";
                    else if (request.ColumnName == "RFID_CHECKOUT_NOT_MATCHING_WITH_DPOS_SALE") status = "SHOW_RFID_CHECKOUT_NOT_MATCHING_WITH_DPOS_SALE_DATA";
                    else if (request.ColumnName == "DPOS_SALE_NOT_MATCHING_WITH_RFID_CHECKOUT") status = "SHOW_DPOS_SALE_NOT_MATCHING_WITH_RFID_CHECKOUT_DATA";
                    else if (request.ColumnName == "TOTAL_MANUAL_SALE") status = "SHOW_MANUAL_SALE_DATA";
                    else if (request.ColumnName == "RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID" || request.ColumnName == "TOTAL_VOID") status = "BIND_DATA_FOR_RFID_CHECKOUT_MATCHING_WITH_DPOS_VOID";

                    if (!string.IsNullOrEmpty(status)) parameters.Add("@status", status);

                    parameters.Add("@SearchTerm", request.SearchTerm ?? "");
                    parameters.Add("@PageIndex", request.PageIndex);
                    parameters.Add("@PageSize", request.PageSize);
                    parameters.Add("@store_code", request.StoreName ?? "");
                    parameters.Add("@fromdate", request.FromDate ?? "");
                    parameters.Add("@todate", request.ToDate ?? "");
                    parameters.Add("@COUNTER_NO", request.Pos ?? "");
                    parameters.Add("@Material", request.ArticleNo ?? "");
                    parameters.Add("@EAN", request.Ean ?? "");
                    parameters.Add("@User_ID", request.UserId ?? "0");
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "ITEM_CD" : request.SortColumn);
                    parameters.Add("@SortDirection", request.SortDirection ?? "asc");

                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Items = items.Select(x => new Dictionary<string, object?>((IDictionary<string, object>)x, StringComparer.OrdinalIgnoreCase)).ToList();

                    response.Summary = new SaleDataSummary
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                        TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                        Qty = parameters.Get<int?>("@QTY") ?? 0
                    };

                    return response;
                }
                catch (Exception)
                {
                    return new SaleDataResponse();
                }
            });
        }
    }
}
