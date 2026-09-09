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

namespace VS_Mart_Backend.Features.Registration.WarehouseRegistration
{
    public class WarehouseRegistrationService : IWarehouseRegistrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<WarehouseRegistrationService> _logger;

        public WarehouseRegistrationService(IConfiguration configuration, ILogger<WarehouseRegistrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        public async Task<IEnumerable<WarehouseListItem>> GetAllWarehousesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_Bind_warehouseMaster");

                var rows = await connection.QueryAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Select(r => new WarehouseListItem
                {
                    WhId = r.WH_ID != null ? (int)r.WH_ID : 0,
                    WhCode = r.Wh_Code?.ToString()?.Trim() ?? string.Empty,
                    WhName = r.Wh_Name?.ToString()?.Trim() ?? string.Empty,
                    WhAddress = r.Wh_Address?.ToString()?.Trim() ?? string.Empty,
                    Status = r.Status?.ToString()?.Trim() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all warehouses.");
                throw;
            }
        }

        public async Task<IEnumerable<WarehouseDropdownItem>> GetWarehouseDropdownAsync(int userId = 0, string userType = "Super Admin")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "SP_DDL_WarehouseID");
                parameters.Add("@User_ID", userId);
                parameters.Add("@User_Type", string.IsNullOrWhiteSpace(userType) ? "Super Admin" : userType.Trim());

                var rows = await connection.QueryAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return rows.Select(r => new WarehouseDropdownItem
                {
                    WhId = r.WH_ID != null ? (int)r.WH_ID : 0,
                    WhName = r.Wh_Name?.ToString()?.Trim() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching warehouse dropdown items.");
                throw;
            }
        }

        public async Task<ActionResponse> CreateWarehouseAsync(CreateWarehouseRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Insert_tbl_Warehouse_Master");
                parameters.Add("@Wh_Code", request.WhCode.Trim());
                parameters.Add("@Wh_Name", request.WhName.Trim());
                parameters.Add("@Wh_Address", request.WhAddress?.Trim() ?? string.Empty);
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
                _logger.LogError(ex, "Error creating warehouse with code {WhCode}.", request.WhCode);
                throw;
            }
        }

        public async Task<ActionResponse> UpdateWarehouseAsync(UpdateWarehouseRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Update_tbl_warehouse_Master");
                parameters.Add("@WH_ID", request.WhId);
                parameters.Add("@Wh_Code", request.WhCode.Trim());
                parameters.Add("@Wh_Name", request.WhName.Trim());
                parameters.Add("@Wh_Address", request.WhAddress?.Trim() ?? string.Empty);
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
                _logger.LogError(ex, "Error updating warehouse with ID {WhId}.", request.WhId);
                throw;
            }
        }

        public async Task<ActionResponse> ToggleWarehouseStatusAsync(int whId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "Delete_tbl_warehouse_Master");
                parameters.Add("@WH_ID", whId);

                await connection.ExecuteAsync("SP_Master", parameters, commandType: CommandType.StoredProcedure);
                return ActionResponse.Ok("Warehouse status toggled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for warehouse ID {WhId}.", whId);
                throw;
            }
        }
    }
}
