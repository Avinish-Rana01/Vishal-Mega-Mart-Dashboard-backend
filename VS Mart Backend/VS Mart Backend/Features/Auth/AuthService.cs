using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;

        public AuthService(IConfiguration configuration, IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _cache = cache;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                string uName = request.UserName?.Trim() ?? "";
                string uPass = request.Password?.Trim() ?? "";

                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_Login");
                parameters.Add("@User_Name", uName);
                parameters.Add("@Password", uPass);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                var cmd = new CommandDefinition(
                    "SP_Master",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                );

                var rawItems = await connection.QueryAsync<dynamic>(cmd);
                var items = rawItems.ToList();

                // 1. Credentials Check (Username or Password mismatch)
                if (items == null || items.Count == 0)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password",
                        ErrorCode = "INVALID_CREDENTIALS"
                    };
                }

                var row = (IDictionary<string, object>)items[0];

                string userType = row.ContainsKey("User_Type") && row["User_Type"] != null ? row["User_Type"].ToString()!.Trim() : string.Empty;
                string userName = row.ContainsKey("User_Name") ? row["User_Name"]?.ToString() ?? request.UserName : request.UserName;
                string userId = row.ContainsKey("User_ID") ? row["User_ID"]?.ToString() ?? "" : "";

                bool isManagerialRole = userType.Equals("Store Admin", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("ZFM", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("Area Manager", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("LP", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("Warehouse Admin", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("WH Admin", StringComparison.OrdinalIgnoreCase);

                bool isWarehouseRole = userType.Equals("Warehouse", StringComparison.OrdinalIgnoreCase)
                    || userType.Equals("Warehouse User", StringComparison.OrdinalIgnoreCase);

                string contactTarget = isManagerialRole 
                    ? "the Super Admin" 
                    : (isWarehouseRole ? "your Warehouse Admin" : "your Store Admin");

                string assignmentType = isWarehouseRole ? "warehouse" : "store";

                // 2. Account Status Check (Deactivated Account)
                int isStatus = 1;
                if (row.ContainsKey("Is_Status") && row["Is_Status"] != null)
                {
                    int.TryParse(row["Is_Status"].ToString(), out isStatus);
                }

                if (isStatus == 0)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = $"Your account has been deactivated. Please contact {contactTarget} for your access.",
                        ErrorCode = "ACCOUNT_DEACTIVATED",
                        UserType = userType,
                        UserName = userName,
                        UserID = userId,
                        IsLoginStatus = "0"
                    };
                }

                // 3. Store / Warehouse Assignment Check
                bool isSuperAdmin = userType.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);

                string loginEligibility = "Allowed";
                if (row.ContainsKey("Login_Eligibility") && row["Login_Eligibility"] != null)
                {
                    loginEligibility = row["Login_Eligibility"].ToString()!.Trim();
                }
                else if (!isSuperAdmin)
                {
                    bool hasStore = row.ContainsKey("Store_ID") && row["Store_ID"] != null && int.TryParse(row["Store_ID"].ToString(), out int sid) && sid > 0;
                    bool hasWh = row.ContainsKey("WH_ID") && row["WH_ID"] != null && int.TryParse(row["WH_ID"].ToString(), out int wid) && wid > 0;
                    if (!hasStore && !hasWh && !isManagerialRole)
                    {
                        loginEligibility = "Not_allowed";
                    }
                }

                if (!isSuperAdmin && loginEligibility.Equals("Not_allowed", StringComparison.OrdinalIgnoreCase))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = $"Welcome to the Vishal Mega Mart, You haven't assigned any {assignmentType} yet. Try contacting {contactTarget} for {assignmentType} assignment.",
                        ErrorCode = "NO_STORE_ASSIGNED",
                        UserType = userType,
                        UserName = userName,
                        UserID = userId,
                        IsLoginStatus = row.ContainsKey("Is_Login_Status") ? row["Is_Login_Status"]?.ToString() ?? "1" : "1"
                    };
                }

                string redirectPage = "Dashboard";
                if (userType.Equals("Dispatch Admin", StringComparison.OrdinalIgnoreCase))
                    redirectPage = "Dispatch_Tracking";
                else if (userType.Equals("Tag Admin", StringComparison.OrdinalIgnoreCase))
                    redirectPage = "Tag_Cleaning";

                var allowedSections = new List<string>();
                string normalizedRole = userType.Trim();

                switch (normalizedRole)
                {
                    case "Super Admin":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take",
                            "dc_validation", "dc_encoding", "tag_management", "vendor_discrepancy",
                            "user_registration", "store_registration", "warehouse_registration", "floor_registration", "tag_cleaning"
                        });
                        break;

                    case "Store Admin":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take", "user_registration", "floor_registration"
                        });
                        break;

                    case "Store User":
                    case "Store":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status"
                        });
                        break;
                    
                    case "LP":
                        allowedSections.AddRange(new[] {
                             "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take"
                        });
                        break;

                    case "ZFM":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take"
                        });
                        break;

                    case "Area Manager":
                        allowedSections.AddRange(new[] {
                             "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take"
                        });
                        break;

                    case "Warehouse Admin":
                    case "WH Admin":
                        allowedSections.AddRange(new[] {
                            "dc_validation", "dc_encoding", "tag_management", "vendor_discrepancy",
                            "user_registration"
                        });
                        break;

                    case "Warehouse User":
                    case "Warehouse":
                    case "WH":
                    case "WH User":
                        allowedSections.AddRange(new[] {
                            "dc_validation", "dc_encoding"
                        });
                        break;

                    case "Dispatch Admin":
                        allowedSections.AddRange(new[] {
                            "dispatch_tracking", "picklist_creation"
                        });
                        break;

                    case "Tag Admin":
                        allowedSections.AddRange(new[] {
                            "tag_cleaning", "get_sap_stock_take"
                        });
                        break;

                    default:
                        break;
                }

                string rawLoginStatus = "1";
                if (row.ContainsKey("Is_Login_Status") && row["Is_Login_Status"] != null)
                {
                    rawLoginStatus = row["Is_Login_Status"].ToString()!.Trim();
                }
                else if (row.ContainsKey("Is_Created_By_Admin_Flag") && row["Is_Created_By_Admin_Flag"] != null)
                {
                    rawLoginStatus = row["Is_Created_By_Admin_Flag"].ToString()!.Trim() == "1" ? "0" : "1";
                }

                bool requirePasswordChange = (rawLoginStatus == "0");

                var response = new LoginResponse
                {
                    Success = true,
                    Message = "Login Successful",
                    UserName = row.ContainsKey("User_Name") ? row["User_Name"]?.ToString() ?? "" : "",
                    UserID = row.ContainsKey("User_ID") ? row["User_ID"]?.ToString() ?? "" : "",
                    UserType = userType,
                    StoreName = row.ContainsKey("STORE_NAME") ? row["STORE_NAME"]?.ToString() ?? "" : (row.ContainsKey("Store_Name") ? row["Store_Name"]?.ToString() ?? "" : ""),
                    WarehouseName = row.ContainsKey("WH_NAME") ? row["WH_NAME"]?.ToString() ?? "" : (row.ContainsKey("Wh_Name") ? row["Wh_Name"]?.ToString() ?? "" : ""),
                    StoreCode = row.ContainsKey("Store_Code") ? row["Store_Code"]?.ToString() ?? "" : (row.ContainsKey("STORE_CODE") ? row["STORE_CODE"]?.ToString() ?? "" : ""),
                    WarehouseCode = row.ContainsKey("WH_Code") ? row["WH_Code"]?.ToString() ?? "" : (row.ContainsKey("Wh_Code") ? row["Wh_Code"]?.ToString() ?? "" : ""),
                    AllowedSections = allowedSections,
                    RedirectPage = redirectPage,
                    RequirePasswordChange = requirePasswordChange,
                    IsLoginStatus = rawLoginStatus
                };

                return response;
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

        public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                string uName = request.UserName?.Trim() ?? "";
                string uId = request.UserId?.Trim() ?? "";
                string currentPass = request.CurrentPassword?.Trim() ?? "";
                string newPass = request.NewPassword?.Trim() ?? "";

                if (string.IsNullOrEmpty(currentPass))
                {
                    return new ChangePasswordResponse { Success = false, Message = "Current password is required." };
                }

                if (string.IsNullOrEmpty(newPass) || newPass.Length < 3)
                {
                    return new ChangePasswordResponse { Success = false, Message = "New password must be at least 3 characters long." };
                }

                if (currentPass == newPass)
                {
                    return new ChangePasswordResponse { Success = false, Message = "New password cannot be identical to the current password." };
                }

                if (string.IsNullOrEmpty(uName) && string.IsNullOrEmpty(uId))
                {
                    return new ChangePasswordResponse { Success = false, Message = "User identifier is required." };
                }

                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Status", "Change_Password");
                parameters.Add("@User_ID", int.TryParse(uId, out int parsedId) ? parsedId : 0);
                parameters.Add("@User_Name", uName);
                parameters.Add("@Password", currentPass);
                parameters.Add("@New_Password", newPass);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                var cmd = new CommandDefinition(
                    "SP_Master",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                );

                await connection.ExecuteAsync(cmd);

                string message = parameters.Get<string>("@Message") ?? "";
                bool isSuccess = message.Equals("Password changed successfully.", StringComparison.OrdinalIgnoreCase);

                if (!isSuccess)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message)
                            ? "Current password is incorrect or user account is not active."
                            : message
                    };
                }

                // Invalidate any cached login entries for this user
                if (!string.IsNullOrEmpty(uName))
                {
                    _cache.Remove($"auth_{uName.ToLowerInvariant()}_{currentPass}");
                }

                return new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Password changed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = $"An error occurred while changing password: {ex.Message}"
                };
            }
        }
    }
}
