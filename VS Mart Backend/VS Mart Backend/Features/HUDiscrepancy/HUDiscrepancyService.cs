using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;
using VS_Mart_Backend.Features.Base;
using VS_Mart_Backend.Features.LiveStockReport;

namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    public class HUDiscrepancyService : BaseDashboardService, IHUDiscrepancyService
    //public class HUDiscrepancyService : BaseDashboardService
    {
        private readonly ILogger<HUDiscrepancyService> _logger;

        public HUDiscrepancyService(IConfiguration configuration, IMemoryCache cache, ILogger<HUDiscrepancyService> logger)
            : base(configuration, cache)
        {
            _logger = logger;
        }
        public async Task<VendorWiseHUDiscrepancyResponse> GetVendorHUDiscrepancyDataAsync(VendorHUDiscrepancyRequest request)
        {
            try
            {
                string connectionString = _connectionString;

                var response = new VendorWiseHUDiscrepancyResponse();

                using SqlConnection con = new SqlConnection(connectionString);

                using SqlCommand cmd = new SqlCommand("SP_NEW_REPORT", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@status", SqlDbType.VarChar).Value = "VIEW_PARK_HU_VENDOR_REPORT";

                cmd.Parameters.Add("@SearchTerm", SqlDbType.VarChar).Value = (object?)request.SearchTerm ?? DBNull.Value;

                cmd.Parameters.Add("@PageIndex", SqlDbType.Int).Value = request.PageIndex;

                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = request.PageSize;

                cmd.Parameters.Add("@fromdate", SqlDbType.VarChar).Value = (object?)request.FromDate ?? DBNull.Value;

                cmd.Parameters.Add("@todate", SqlDbType.VarChar).Value = (object?)request.ToDate ?? DBNull.Value;

                cmd.Parameters.Add("@Vendor_Code", SqlDbType.VarChar).Value = (object?)request.VendorCode ?? DBNull.Value;

                cmd.Parameters.Add("@SortColumn", SqlDbType.VarChar).Value = string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn;

                cmd.Parameters.Add("@SortDirection", SqlDbType.VarChar).Value = string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection;

                // Output parameters
                SqlParameter recordCountParam = cmd.Parameters.Add("@RecordCount", SqlDbType.Int);
                recordCountParam.Direction = ParameterDirection.Output;

                SqlParameter totalCountParam = cmd.Parameters.Add("@TotalCount", SqlDbType.Int);
                totalCountParam.Direction = ParameterDirection.Output;

                SqlParameter scanQtyParam = cmd.Parameters.Add("@SCANQTY", SqlDbType.Int);
                scanQtyParam.Direction = ParameterDirection.Output;

                SqlParameter actualQtyParam = cmd.Parameters.Add("@ACTUALQTY", SqlDbType.Int);
                actualQtyParam.Direction = ParameterDirection.Output;

                SqlParameter diffQtyParam = cmd.Parameters.Add("@DIFFQTY", SqlDbType.Int);
                diffQtyParam.Direction = ParameterDirection.Output;

                SqlParameter excessQtyParam = cmd.Parameters.Add("@Excess_Qty", SqlDbType.Int);
                excessQtyParam.Direction = ParameterDirection.Output;

                SqlParameter huCountParam = cmd.Parameters.Add("@HUCOUNT", SqlDbType.Int);
                huCountParam.Direction = ParameterDirection.Output;

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    response.Data.Add(row);
                }

                await reader.CloseAsync();

                // Read output parameters AFTER reader is closed
                response.PageIndex = request.PageIndex;

                response.RecordCount = recordCountParam.Value == DBNull.Value ? 0 : Convert.ToInt32(recordCountParam.Value);

                response.TotalCount = totalCountParam.Value == DBNull.Value ? 0 : Convert.ToInt32(totalCountParam.Value);

                response.ScannedQty = scanQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(scanQtyParam.Value);

                response.ActualQty = actualQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(actualQtyParam.Value);

                response.DifferenceQty = diffQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(diffQtyParam.Value);

                response.ExcessQty = excessQtyParam.Value == DBNull.Value ? 0 : Convert.ToInt32(excessQtyParam.Value);

                response.HUCount = huCountParam.Value == DBNull.Value ? 0 : Convert.ToInt32(huCountParam.Value);

                return response;
            }
            catch (Exception ex)
            {
                return new VendorWiseHUDiscrepancyResponse();
            }

        }

        public async Task<ApiResponse> StoreandUserData(StoreUserRequest req)
        {
            try
            {
                string ConnectionString = _configuration.GetConnectionString("POS");

                using var conn = new SqlConnection(ConnectionString);
                using var cmd = new SqlCommand("SP_Master", conn)  // <-- your actual SP name
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Helper to add nullable params cleanly
                void AddParam(string name, object value, SqlDbType type)
                {
                    var p = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
                    cmd.Parameters.Add(p);
                }

                AddParam("@status", req.Status, SqlDbType.VarChar);

                if (req.Status.Contains("Store", StringComparison.OrdinalIgnoreCase))
                {
                    // Store Master
                    AddParam("@Store_ID", req.Store_ID, SqlDbType.Int);
                    AddParam("@Store_Code", req.Store_Code, SqlDbType.VarChar);
                    AddParam("@Store_Name", req.Store_Name, SqlDbType.VarChar);
                    AddParam("@MAIL_ID", req.MAIL_ID, SqlDbType.VarChar);
                    AddParam("@State_ID", req.State_ID, SqlDbType.Int);
                    AddParam("@City_ID", req.City_ID, SqlDbType.Int);
                    AddParam("@SM_ID", req.SM_ID, SqlDbType.Int);
                    AddParam("@AM_ID", req.AM_ID, SqlDbType.Int);
                    AddParam("@ZFM_ID", req.ZFM_ID, SqlDbType.Int);
                    AddParam("@LP_ID", req.LP_ID, SqlDbType.Int);
                    AddParam("@Entry_By", req.Entry_By, SqlDbType.Int);
                    AddParam("@Modify_By", req.Modify_By, SqlDbType.Int);
                }
                else if (req.Status.Contains("user", StringComparison.OrdinalIgnoreCase))
                {
                    // User Registration
                    AddParam("@User_ID", req.User_ID, SqlDbType.Int);
                    AddParam("@User_Name", req.User_Name, SqlDbType.VarChar);
                    AddParam("@Password", req.Password, SqlDbType.VarChar);
                    AddParam("@User_Type", req.User_Type, SqlDbType.VarChar);
                    AddParam("@WH_ID", req.WH_ID, SqlDbType.Int);
                    AddParam("@Role_ID", req.Role_ID, SqlDbType.Int);
                    AddParam("@Emp_ID", req.Emp_ID, SqlDbType.Int);
                    AddParam("@Reader_Config_ID", req.Reader_Config_ID, SqlDbType.Int);
                }

                // Output parameter for @Message
                var messageParam = new SqlParameter("@Message", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };
                cmd.Parameters.Add(messageParam);

                var dt = new DataTable();

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // Loads a result set if the branch produces one (e.g. a "Get" action)
                    if (reader.HasRows)
                        dt.Load(reader);
                }

                string message = messageParam.Value != DBNull.Value ? messageParam.Value.ToString() : null;

                return new ApiResponse
                {
                    Success = true,
                    Message = message,
                    Data = JsonConvert.SerializeObject(dt)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse();
            }

        }
    }
}
