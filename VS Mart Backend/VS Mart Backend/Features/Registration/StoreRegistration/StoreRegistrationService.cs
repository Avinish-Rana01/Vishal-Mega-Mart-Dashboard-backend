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

namespace VS_Mart_Backend.Features.Registration.StoreRegistration
{
    public class StoreRegistrationService : IStoreRegistrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<StoreRegistrationService> _logger;

        public StoreRegistrationService(IConfiguration configuration, ILogger<StoreRegistrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        public async Task<IEnumerable<StoreListItem>> GetAllStoresAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_Bind_StoreMaster");

                var rows = await connection.QueryAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Select(r => new StoreListItem
                {
                    StoreId = r.Store_ID != null ? (int)r.Store_ID : 0,
                    StoreCode = r.Store_Code?.ToString()?.Trim() ?? string.Empty,
                    StoreName = r.Store_Name?.ToString()?.Trim() ?? string.Empty,
                    Status = r.Status?.ToString()?.Trim() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all stores.");
                throw;
            }
        }

        public async Task<IEnumerable<StoreDropdownItem>> GetStoreDropdownAsync(int userId = 0, string userType = "Super Admin")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_DDL_StoreID");
                parameters.Add("@User_ID", userId);
                parameters.Add("@User_Type", string.IsNullOrWhiteSpace(userType) ? "Super Admin" : userType.Trim());

                var rows = await connection.QueryAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Select(r => new StoreDropdownItem
                {
                    StoreId = r.Store_ID != null ? (int)r.Store_ID : 0,
                    StoreName = r.Store_Name?.ToString()?.Trim() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store dropdown items.");
                throw;
            }
        }

        public async Task<ActionResponse> CreateStoreAsync(CreateStoreRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Insert_tbl_Store_Master");
                parameters.Add("@Store_Code", request.StoreCode.Trim());
                parameters.Add("@Store_Name", request.StoreName.Trim());
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
                _logger.LogError(ex, "Error creating store with code {StoreCode}.", request.StoreCode);
                throw;
            }
        }

        public async Task<ActionResponse> UpdateStoreAsync(UpdateStoreRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Update_tbl_Store_Master");
                parameters.Add("@Store_ID", request.StoreId);
                parameters.Add("@Store_Code", request.StoreCode.Trim());
                parameters.Add("@Store_Name", request.StoreName.Trim());
                parameters.Add("@Modify_By", request.ModifyBy);
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
                _logger.LogError(ex, "Error updating store with ID {StoreId}.", request.StoreId);
                throw;
            }
        }

        public async Task<ActionResponse> ToggleStoreStatusAsync(int storeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Delete_tbl_Store_Master");
                parameters.Add("@Store_ID", storeId);

                await connection.ExecuteAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return ActionResponse.Ok("Store status toggled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for store ID {StoreId}.", storeId);
                throw;
            }
        }
    }
}
