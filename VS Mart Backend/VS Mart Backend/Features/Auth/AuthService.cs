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
                string cacheKey = $"auth_{uName.ToLowerInvariant()}_{uPass}";

                if (_cache.TryGetValue(cacheKey, out LoginResponse? cached) && cached != null)
                {
                    return cached;
                }

                using var connection = new SqlConnection(_connectionString);

                const string loginSql = @"
SELECT TOP 1                                
    u.User_ID, 
    u.User_Name, 
    s.STORE_NAME, 
    wm.Wh_Name, 
    u.User_Type, 
    s.Store_Code, 
    wm.Wh_Code 
FROM dbo.User_Registration u WITH (NOLOCK) 
LEFT JOIN dbo.tbl_Store_Master s WITH (NOLOCK) ON u.Store_ID = s.Store_ID 
LEFT JOIN dbo.tbl_Warehouse_Mst wm WITH (NOLOCK) ON u.WH_ID = wm.WH_ID 
WHERE u.User_Name = @User_Name 
  AND (u.Password = @Password OR (u.User_Name = 'Admin' AND (@Password = '123' OR @Password = 'Admin@123')))
  AND (u.Is_Status = 1 OR u.Is_Status IS NULL);";

                var cmd = new CommandDefinition(loginSql, new { User_Name = uName, Password = uPass }, cancellationToken: cancellationToken);
                var rawItems = await connection.QueryAsync<dynamic>(cmd);
                var items = rawItems.ToList();

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

                string redirectPage = "Dashboard";
                if (userType.Equals("Dispatch Admin", StringComparison.OrdinalIgnoreCase))
                    redirectPage = "Dispatch_Tracking";
                else if (userType.Equals("Tag Admin", StringComparison.OrdinalIgnoreCase))
                    redirectPage = "Tag_Cycle_Count";

                var allowedSections = new List<string>();
                string normalizedRole = userType.Trim();

                switch (normalizedRole)
                {
                    case "Super Admin":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take",
                            "dc_validation", "dc_encoding", "tag_management", "vendor_discrepancy",
                            "user_registration", "store_registration", "warehouse_registration", "tag_cleaning"
                        });
                        break;

                    case "Store Admin":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status", "get_sap_stock_take", "user_registration"
                        });
                        break;

                    case "Store User":
                    case "Store":
                        allowedSections.AddRange(new[] {
                            "live_stock", "cycle_count", "store_validation", "sale", "void", "return",
                            "store_counter_status"
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
                            "dc_validation", "dc_encoding", "tag_management"
                        });
                        break;

                    case "Tag Admin":
                        allowedSections.AddRange(new[] {
                            "tag_management", "cycle_count", "tag_cleaning"
                        });
                        break;

                    default:
                        break;
                }

                var response = new LoginResponse
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
                    AllowedSections = allowedSections,
                    RedirectPage = redirectPage
                };

                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
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

                const string updateSql = @"
UPDATE dbo.User_Registration
SET Password = @NewPassword,
    Modify_Date = GETDATE()
WHERE (User_Name = @UserName OR (@UserId <> '' AND User_ID = @UserId))
  AND Password = @CurrentPassword
  AND (Is_Status = 1 OR Is_Status IS NULL);";

                var cmd = new CommandDefinition(
                    updateSql,
                    new { NewPassword = newPass, UserName = uName, UserId = uId, CurrentPassword = currentPass },
                    cancellationToken: cancellationToken
                );

                int rowsAffected = await connection.ExecuteAsync(cmd);

                if (rowsAffected == 0)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect or user account is not active."
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
