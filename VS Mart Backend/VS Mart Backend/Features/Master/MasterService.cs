using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;
using VS_Mart_Backend.Services;

namespace VS_Mart_Backend.Features.Master
{
    public class MasterService : IMasterService
    {
        private readonly string _connectionString;
        private readonly ILogger<MasterService> _logger;
        private readonly IMemoryCache _cache;

        public MasterService(IConfiguration configuration, ILogger<MasterService> logger, IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
            _cache = cache;
        }

        public async Task<MasterResponse> ExecuteMasterAsync(MasterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return new MasterResponse
                {
                    Success = false,
                    Message = "Status parameter is required."
                };
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                // Bind all parameters supported by SP_Master
                parameters.Add("@Status", request.Status.Trim());
                parameters.Add("@Store_Code", request.Store_Code?.Trim() ?? string.Empty);
                parameters.Add("@Store_Name", request.Store_Name?.Trim() ?? string.Empty);
                parameters.Add("@Entry_By", request.Entry_By);
                parameters.Add("@Is_Status", request.Is_Status);
                parameters.Add("@Reader_Id", request.Reader_Id);
                parameters.Add("@Reader_Name", request.Reader_Name?.Trim() ?? string.Empty);
                parameters.Add("@Reader_MAC", request.Reader_MAC?.Trim() ?? string.Empty);
                parameters.Add("@Antena", request.Antena);
                parameters.Add("@User_Name", request.User_Name?.Trim() ?? string.Empty);
                parameters.Add("@Password", request.Password?.Trim() ?? string.Empty);
                parameters.Add("@User_Type", request.User_Type?.Trim() ?? string.Empty);
                parameters.Add("@Reader_Config_ID", request.Reader_Config_ID);
                parameters.Add("@Modify_By", request.Modify_By);
                parameters.Add("@Store_ID", request.Store_ID);
                parameters.Add("@User_ID", request.User_ID);
                parameters.Add("@Device_ESN", request.Device_ESN?.Trim() ?? string.Empty);
                parameters.Add("@Encode_DateTime", request.Encode_DateTime?.Trim() ?? string.Empty);
                parameters.Add("@WH_ID", request.WH_ID);
                parameters.Add("@Wh_Code", request.Wh_Code?.Trim() ?? string.Empty);
                parameters.Add("@Wh_Name", request.Wh_Name?.Trim() ?? string.Empty);
                parameters.Add("@Wh_Address", request.Wh_Address?.Trim() ?? string.Empty);
                parameters.Add("@Store_Floor_ID", request.Store_Floor_ID);
                parameters.Add("@Store_Floor", request.Store_Floor?.Trim() ?? string.Empty);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                var rows = (await connection.QueryAsync<dynamic>(
                    "SP_Master", 
                    parameters, 
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).ToList();

                string message = parameters.Get<string>("@Message")?.Trim() ?? string.Empty;

                bool isSuccess = true;
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("INVALID USER", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("INCORRECT PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("USER IS INACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        isSuccess = false;
                    }
                }

                if (string.IsNullOrEmpty(message))
                {
                    message = rows.Count > 0 ? $"Retrieved {rows.Count} record(s) successfully." : "Operation executed.";
                }

                // If store status was updated/deleted/inserted, immediately evict Tier 2 caches and trigger background refresh
                if (isSuccess && !string.IsNullOrEmpty(request.Status) &&
                    (request.Status.Equals("Delete_tbl_Store_Master", StringComparison.OrdinalIgnoreCase) ||
                     request.Status.Equals("Update_tbl_Store_Master", StringComparison.OrdinalIgnoreCase) ||
                     request.Status.Equals("Insert_tbl_Store_Master", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("MasterService: Store master modified with status {Status}. Evicting Tier 2 dashboard caches and triggering warmup.", request.Status);

                    // Evict ONLY Tier 2 cache keys (leaving Tier 1 untouched as agreed)
                    BaseDashboardService.InvalidateKeysByPrefix(_cache, 
                        "SaleDashboard_Master_", 
                        "VoidDashboard_Master_", 
                        "ReturnDashboard_Master_", 
                        "TagCycleCount_Master_");

                    // Trigger collision-free Tier 2 warmup with timer reset
                    CacheWarmerService.TriggerTier2WarmupWithReset();
                }

                return new MasterResponse
                {
                    Success = isSuccess,
                    Message = message,
                    Data = rows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SP_Master with Status: {Status}", request.Status);
                throw;
            }
        }
    }
}
