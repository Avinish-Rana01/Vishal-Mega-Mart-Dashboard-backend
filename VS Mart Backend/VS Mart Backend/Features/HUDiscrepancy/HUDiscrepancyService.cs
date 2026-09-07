using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;

namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    public class HUDiscrepancyService : BaseDashboardService, IHUDiscrepancyService
    {
        private readonly ILogger<HUDiscrepancyService> _logger;

        public HUDiscrepancyService(IConfiguration configuration, IMemoryCache cache, ILogger<HUDiscrepancyService> logger)
            : base(configuration, cache)
        {
            _logger = logger;
        }

        public async Task<VendorWiseHUDiscrepancyResponse> GetVendorHUDiscrepancyDataAsync(VendorHUDiscrepancyRequest request)
        {
            string cacheKey = $"VendorHUDiscrepancy_{request.VendorCode}_{request.SearchTerm}_{request.FromDate}_{request.ToDate}_{request.PageIndex}_{request.PageSize}_{request.SortColumn}_{request.SortDirection}";

            return await GetOrCreateWithSWRAsync(cacheKey, async () =>
            {
                try
                {
                    var response = new VendorWiseHUDiscrepancyResponse();
                    using var connection = new SqlConnection(_connectionString);
                    var parameters = new DynamicParameters();

                    parameters.Add("@status", "VIEW_PARK_HU_VENDOR_REPORT", DbType.String, size: 50);
                    parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String, size: 200);
                    parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);
                    parameters.Add("@PageSize", request.PageSize, DbType.Int32);
                    parameters.Add("@fromdate", request.FromDate ?? "", DbType.String, size: 20);
                    parameters.Add("@todate", request.ToDate ?? "", DbType.String, size: 20);
                    parameters.Add("@Vendor_Code", request.VendorCode ?? "", DbType.String, size: 50);
                    parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String, size: 50);
                    parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String, size: 10);

                    // Output parameters
                    parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@SCANQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@Excess_Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    parameters.Add("@HUCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var items = await connection.QueryAsync<dynamic>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                    response.Data = items
                        .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    response.PageIndex = request.PageIndex;
                    response.RecordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                    response.TotalCount = parameters.Get<int?>("@TotalCount") ?? 0;
                    response.ScannedQty = parameters.Get<int?>("@SCANQTY") ?? 0;
                    response.ActualQty = parameters.Get<int?>("@ACTUALQTY") ?? 0;
                    response.DifferenceQty = parameters.Get<int?>("@DIFFQTY") ?? 0;
                    response.ExcessQty = parameters.Get<int?>("@Excess_Qty") ?? 0;
                    response.HUCount = parameters.Get<int?>("@HUCOUNT") ?? 0;

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GetVendorHUDiscrepancyDataAsync");
                    return new VendorWiseHUDiscrepancyResponse();
                }
            });
        }

        public async Task<ApiResponse> StoreandUserData(StoreUserRequest req)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@status", req.Status, DbType.String, size: 100);

                if (!string.IsNullOrEmpty(req.Status) && req.Status.Contains("Store", StringComparison.OrdinalIgnoreCase))
                {
                    // Store Master
                    parameters.Add("@Store_ID", req.Store_ID, DbType.Int32);
                    parameters.Add("@Store_Code", req.Store_Code ?? "", DbType.String, size: 100);
                    parameters.Add("@Store_Name", req.Store_Name ?? "", DbType.String, size: 200);
                    parameters.Add("@MAIL_ID", req.MAIL_ID ?? "", DbType.String, size: 100);
                    parameters.Add("@State_ID", req.State_ID, DbType.Int32);
                    parameters.Add("@City_ID", req.City_ID, DbType.Int32);
                    parameters.Add("@SM_ID", req.SM_ID, DbType.Int32);
                    parameters.Add("@AM_ID", req.AM_ID, DbType.Int32);
                    parameters.Add("@ZFM_ID", req.ZFM_ID, DbType.Int32);
                    parameters.Add("@LP_ID", req.LP_ID, DbType.Int32);
                    parameters.Add("@Entry_By", req.Entry_By, DbType.Int32);
                    parameters.Add("@Modify_By", req.Modify_By, DbType.Int32);
                }
                else if (!string.IsNullOrEmpty(req.Status) && req.Status.Contains("user", StringComparison.OrdinalIgnoreCase))
                {
                    // User Registration
                    parameters.Add("@User_ID", req.User_ID, DbType.Int32);
                    parameters.Add("@User_Name", req.User_Name ?? "", DbType.String, size: 100);
                    parameters.Add("@Password", req.Password ?? "", DbType.String, size: 100);
                    parameters.Add("@User_Type", req.User_Type ?? "", DbType.String, size: 50);
                    parameters.Add("@WH_ID", req.WH_ID, DbType.Int32);
                    parameters.Add("@Role_ID", req.Role_ID, DbType.Int32);
                    parameters.Add("@Emp_ID", req.Emp_ID, DbType.Int32);
                    parameters.Add("@Reader_Config_ID", req.Reader_Config_ID, DbType.Int32);
                }

                // Output parameter for @Message
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.InputOutput);

                var items = (await conn.QueryAsync<dynamic>("SP_Master", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120))
                    .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                string? message = parameters.Get<string>("@Message");

                return new ApiResponse
                {
                    Success = true,
                    Message = message,
                    Data = JsonConvert.SerializeObject(items)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StoreandUserData");
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
