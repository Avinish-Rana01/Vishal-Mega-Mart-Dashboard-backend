using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace VS_Mart_Backend.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
        }

        public LoginResponse Login(LoginRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@User_Name", request.UserName?.Trim() ?? "");
                parameters.Add("@Password", request.Password?.Trim() ?? "");
                parameters.Add("@Status", "SP_Login");
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                var items = connection.Query<dynamic>("SP_Master", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120).ToList();

                string dbMessage = parameters.Get<string>("@Message") ?? string.Empty;

                if (items == null || items.Count == 0)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                var row = (IDictionary<string, object>)items[0];
                string userType = row.ContainsKey("User_Type") && row["User_Type"] != null ? row["User_Type"].ToString()!.Trim() : string.Empty;

                if (userType == "Store" || userType == "Warehouse")
                {
                    throw new UnauthorizedAccessException("Forbidden user type");
                }

                string redirectPage = "Dashboard";
                if (userType == "Dispatch Admin")
                    redirectPage = "Dispatch_Tracking";
                else if (userType == "Tag Admin")
                    redirectPage = "Tag_Cycle_Count";

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login Successful",
                    UserName = row.ContainsKey("User_Name") ? row["User_Name"]?.ToString() ?? "" : "",
                    UserID = row.ContainsKey("User_ID") ? row["User_ID"]?.ToString() ?? "" : "",
                    UserType = userType,
                    StoreName = row.ContainsKey("STORE_NAME") ? row["STORE_NAME"]?.ToString() ?? "" : "",
                    WarehouseName = row.ContainsKey("WH_NAME") ? row["WH_NAME"]?.ToString() ?? "" : "",
                    StoreCode = row.ContainsKey("Store_Code") ? row["Store_Code"]?.ToString() ?? "" : "",
                    WarehouseCode = row.ContainsKey("WH_Code") ? row["WH_Code"]?.ToString() ?? "" : "",
                    RedirectPage = redirectPage
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login."
                };
            }
        }
    }
}
