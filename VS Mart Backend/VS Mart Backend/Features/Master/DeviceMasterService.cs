using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Master
{
    public class DeviceMasterService : IDeviceMasterService
    {
        private readonly string _connectionString;

        public DeviceMasterService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
        }

        public async Task<IEnumerable<DeviceItemDto>> GetDevicesAsync(int roleId = 4, int userId = 1, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Status", "BIND_DEVICE_MASTER");
            parameters.Add("@Role_ID", roleId);
            parameters.Add("@User_ID", userId);

            var cmd = new CommandDefinition(
                "SP_Device_Master",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            return await connection.QueryAsync<DeviceItemDto>(cmd);
        }

        public async Task<IEnumerable<DeviceTypeDropdownDto>> GetDeviceTypesAsync(CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Status", "BIND_DEVICE_TYPE_DROPDOWN");

            var cmd = new CommandDefinition(
                "SP_Device_Master",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            return await connection.QueryAsync<DeviceTypeDropdownDto>(cmd);
        }

        public async Task<DeviceDropdownsDto> GetDropdownsAsync(CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var dto = new DeviceDropdownsDto();

            // 1. Device Types
            var dtParams = new DynamicParameters();
            dtParams.Add("@Status", "BIND_DEVICE_TYPE_DROPDOWN");
            var deviceTypes = await connection.QueryAsync<DeviceTypeDropdownDto>(
                new CommandDefinition("SP_Device_Master", dtParams, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken)
            );
            dto.DeviceTypes = deviceTypes.ToList();

            // 2. Stores
            var storeParams = new DynamicParameters();
            storeParams.Add("@Status", "BIND_STORE_DROPDOWN");
            var stores = await connection.QueryAsync<dynamic>(
                new CommandDefinition("SP_Device_Master", storeParams, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken)
            );
            dto.Stores = stores.ToList();

            // 3. Warehouses
            var whParams = new DynamicParameters();
            whParams.Add("@Status", "BIND_WAREHOUSE_DROPDOWN");
            var warehouses = await connection.QueryAsync<dynamic>(
                new CommandDefinition("SP_Device_Master", whParams, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken)
            );
            dto.Warehouses = warehouses.ToList();

            return dto;
        }

        public async Task<(bool Success, string Message)> SaveDeviceAsync(SaveDeviceRequest request, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            string status = request.Device_ID > 0 ? "UPDATE_DEVICE" : "INSERT_DEVICE";

            parameters.Add("@Status", status);
            parameters.Add("@Device_ID", request.Device_ID, DbType.Int32);
            parameters.Add("@Store_ID", request.Store_ID > 0 ? request.Store_ID : null, DbType.Int32);
            parameters.Add("@WH_ID", request.WH_ID > 0 ? request.WH_ID : null, DbType.Int32);
            parameters.Add("@Cash_Counter_No", string.IsNullOrWhiteSpace(request.Cash_Counter_No) ? null : request.Cash_Counter_No.Trim(), DbType.String);
            parameters.Add("@Device_Type_ID", request.Device_Type_ID, DbType.Int32);
            parameters.Add("@Device_ESN", request.Device_ESN?.Trim(), DbType.String);
            parameters.Add("@User_ID", request.User_ID, DbType.Int32);
            parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

            var cmd = new CommandDefinition(
                "SP_Device_Master",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(cmd);
            string message = parameters.Get<string>("@Message") ?? "Operation completed.";
            bool success = message.Contains("success", StringComparison.OrdinalIgnoreCase);

            return (success, message);
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Status", "TOGGLE_DEVICE_STATUS");
            parameters.Add("@Device_ID", deviceId, DbType.Int32);
            parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

            var cmd = new CommandDefinition(
                "SP_Device_Master",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(cmd);
            string message = parameters.Get<string>("@Message") ?? "Status updated.";
            bool success = message.Contains("success", StringComparison.OrdinalIgnoreCase);

            return (success, message);
        }
    }
}

