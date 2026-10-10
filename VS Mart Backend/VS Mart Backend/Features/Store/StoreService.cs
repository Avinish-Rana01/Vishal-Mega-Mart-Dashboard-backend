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

                // 1. Resolve user profile to check role and assigned store
                var userProfile = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        u.User_ID, 
                        ISNULL(u.User_Type, '') AS User_Type, 
                        ISNULL(u.Store_ID, 0) AS Store_ID,
                        ISNULL(sm.Store_Code, '') AS Store_Code,
                        ISNULL(sm.Store_Name, '') AS Store_Name
                    FROM dbo.User_Registration u WITH (NOLOCK)
                    LEFT JOIN dbo.tbl_Store_Master sm WITH (NOLOCK) ON u.Store_ID = sm.Store_ID
                    WHERE u.User_ID = @UserId", new { UserId = userId });

                string userType = userProfile != null ? Convert.ToString(userProfile.User_Type)?.Trim() ?? "" : "";
                int assignedStoreId = userProfile != null ? Convert.ToInt32(userProfile.Store_ID) : 0;
                bool isSuperAdmin = userType.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);

                // 2. Fetch distinct stores with configured active cash counters
                var parameters = new DynamicParameters();
                parameters.Add("@status", "STORENAME_FOR_COUNTER_STATUS");
                parameters.Add("@User_ID", userId);
                parameters.Add("@CounterRoleid", isSuperAdmin ? "4" : (assignedStoreId > 0 ? "5" : "4"));
                parameters.Add("@CounterEmpID", userId);
                parameters.Add("@CounterStoreID", assignedStoreId);

                var allStores = (await connection.QueryAsync<CounterStatusStoreDto>(
                    "SP_Master",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60
                )).ToList();

                List<CounterStatusStoreDto> filteredStores;

                if (isSuperAdmin || (assignedStoreId <= 0 && userType.Equals("Store Admin", StringComparison.OrdinalIgnoreCase)))
                {
                    // Super Admin or roving Store Admin without a single store lock sees all available stores
                    filteredStores = allStores;
                }
                else if (assignedStoreId > 0)
                {
                    // Store-restricted user (Store Admin or Store User with an assigned store) only sees their authorized store
                    filteredStores = allStores.Where(s => s.Store_ID == assignedStoreId).ToList();

                    // Fallback: If SP_Master didn't return their assigned store (e.g. no devices active yet), include it from store profile
                        string? fallbackStoreName = Convert.ToString(userProfile?.Store_Name);
                        if (!string.IsNullOrWhiteSpace(fallbackStoreName))
                        {
                            filteredStores.Add(new CounterStatusStoreDto
                            {
                                Store_ID = assignedStoreId,
                                Store_Name = fallbackStoreName
                            });
                        }
                }
                else
                {
                    // Other roles without assigned stores or permissions
                    filteredStores = isSuperAdmin ? allStores : new List<CounterStatusStoreDto>();
                }

                return new CounterStatusResponse<CounterStatusStoreDto>
                {
                    Success = true,
                    Message = filteredStores.Count > 0 ? $"Retrieved {filteredStores.Count} store(s) successfully." : "No stores found for this user account.",
                    Data = filteredStores
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores for counter status for userId {UserId}.", userId);
                throw;
            }
        }

        public async Task<CounterStatusResponse<CounterStatusDetailDto>> GetCounterStatusDetailsAsync(int storeId, int userId = 0)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "COUNTER_STATUS_DETAILS");
                parameters.Add("@Store_ID", storeId);
                parameters.Add("@User_ID", userId);

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
