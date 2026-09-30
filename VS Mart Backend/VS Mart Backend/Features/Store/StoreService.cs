using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Store
{
    public class StoreService : IStoreService
    {
        private readonly string _connectionString;
        private readonly ILogger<StoreService> _logger;

        public StoreService(IConfiguration configuration, ILogger<StoreService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }

        public async Task<CounterStatusResponse<CounterStatusStoreDto>> GetCounterStatusStoresAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "STORENAME_FOR_COUNTER_STATUS");
                parameters.Add("@User_ID", userId);

                var rows = (await connection.QueryAsync<CounterStatusStoreDto>(
                    "SP_Master",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60
                )).ToList();

                return new CounterStatusResponse<CounterStatusStoreDto>
                {
                    Success = true,
                    Message = rows.Count > 0 ? $"Retrieved {rows.Count} store(s) successfully." : "No stores found.",
                    Data = rows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores for counter status for userId {UserId}.", userId);
                throw;
            }
        }

        public async Task<CounterStatusResponse<CounterStatusDetailDto>> GetCounterStatusDetailsAsync(int storeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "COUNTER_STATUS_DETAILS");
                parameters.Add("@Store_ID", storeId);

                var rows = (await connection.QueryAsync<CounterStatusDetailDto>(
                    "SP_Master",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60
                )).ToList();

                return new CounterStatusResponse<CounterStatusDetailDto>
                {
                    Success = true,
                    Message = rows.Count > 0 ? $"Retrieved {rows.Count} counter status record(s) successfully." : "No counter data found for the selected store.",
                    Data = rows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching counter status details for storeId {StoreId}.", storeId);
                throw;
            }
        }
    }
}
