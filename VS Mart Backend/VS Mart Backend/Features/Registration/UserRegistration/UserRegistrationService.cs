using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.UserRegistration
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserRegistrationService> _logger;

        public UserRegistrationService(IConfiguration configuration, ILogger<UserRegistrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        public async Task<IEnumerable<UserListItem>> GetUsersAsync(int userId = 0, string userType = "Super Admin")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_Bind_User_Master");
                parameters.Add("@User_ID", userId);
                parameters.Add("@User_Type", string.IsNullOrWhiteSpace(userType) ? "Super Admin" : userType.Trim());

                var rows = await connection.QueryAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Select(r => new UserListItem
                {
                    UserId = r.User_ID != null ? (int)r.User_ID : 0,
                    UserName = r.User_Name?.ToString()?.Trim() ?? string.Empty,
                    Password = r.Password?.ToString()?.Trim() ?? string.Empty,
                    UserType = r.User_Type?.ToString()?.Trim() ?? string.Empty,
                    StoreId = r.Store_ID != null ? (int?)r.Store_ID : null,
                    StoreName = r.Store_Name?.ToString()?.Trim() ?? string.Empty,
                    WarehouseName = r.Warehouse_Name?.ToString()?.Trim() ?? string.Empty,
                    Status = r.Status?.ToString()?.Trim() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user list.");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "BIND_USER_TYPE");

                var rows = await connection.QueryAsync<string>("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user roles.");
                throw;
            }
        }

        public async Task<ActionResponse> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Insert_user_registration");
                parameters.Add("@User_Name", request.UserName.Trim());
                parameters.Add("@Password", request.Password.Trim());
                parameters.Add("@User_Type", request.UserType.Trim());
                parameters.Add("@Store_ID", request.StoreId);
                parameters.Add("@WH_ID", request.WhId);
                parameters.Add("@Entry_By", request.EntryBy);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);

                await connection.ExecuteAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                string message = parameters.Get<string>("@Message") ?? string.Empty;
                bool isSuccess = message.Contains("successfully", StringComparison.OrdinalIgnoreCase);

                return new ActionResponse
                {
                    Success = isSuccess,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserName}.", request.UserName);
                throw;
            }
        }

        public async Task<ActionResponse> UpdateUserAsync(UpdateUserRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Update_user_registration");
                parameters.Add("@User_ID", request.UserId);
                parameters.Add("@User_Name", request.UserName.Trim());
                parameters.Add("@Password", request.Password.Trim());
                parameters.Add("@User_Type", request.UserType.Trim());
                parameters.Add("@Store_ID", request.StoreId);
                parameters.Add("@WH_ID", request.WhId);
                parameters.Add("@Modify_By", request.ModifyBy);

                await connection.ExecuteAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);

                return ActionResponse.Ok("User details updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", request.UserId);
                throw;
            }
        }

        public async Task<ActionResponse> ToggleUserStatusAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Delete_user_registration");
                parameters.Add("@User_ID", userId);

                await connection.ExecuteAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);

                return ActionResponse.Ok("User status toggled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user {UserId}.", userId);
                throw;
            }
        }
    }
}
