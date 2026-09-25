using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using VS_Mart_Backend.Features.Master;

namespace VS_Mart_Backend.Features.Dashboard.TagManagement
{
    public class TagmanagementService : ITagManagement
    {
        private readonly string _connectionString;
        private readonly ILogger<TagmanagementService> _logger;

        public TagmanagementService(IConfiguration configuration, ILogger<TagmanagementService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }


        public async Task<TagDetailsResponse> GetTagDetailsAsync(TagDetailsRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();

                // Input parameters
                parameters.Add("@status", "TAG_MANAGEMENT_LOCATION", DbType.String);

                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);

                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);

                parameters.Add("@PageSize", request.PageSize, DbType.Int32);

                parameters.Add("@SortColumn", string.IsNullOrWhiteSpace(request.SortColumn) ? "CYCLE_COUNT" : request.SortColumn, DbType.String);

                parameters.Add("@SortDirection", string.IsNullOrWhiteSpace(request.SortDirection) ? "desc" : request.SortDirection, DbType.String);


                // Output parameters
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@STORECOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@WHCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);

                // Execute stored procedure and read multiple result sets
                using var multi = await connection.QueryMultipleAsync("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120);

                var data = (await multi.ReadAsync<TagDetailsData>()).ToList();
                var storeInventory = new List<TagStoreInventoryData>();

                if (!multi.IsConsumed)
                {
                    storeInventory = (await multi.ReadAsync<TagStoreInventoryData>()).ToList();
                }

                // Get output values
                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                int cycleCount = parameters.Get<int?>("@QTY") ?? 0;
                int storeCount = parameters.Get<int?>("@STORECOUNT") ?? 0;
                int whCount = parameters.Get<int?>("@WHCOUNT") ?? 0;

                // Build response
                return new TagDetailsResponse
                {
                    TagData = data,
                    StoreInventory = storeInventory,
                    Pager = new TagDetailsPager
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = recordCount,
                        CycleCount = cycleCount,
                        StoreCount = storeCount,
                        WhCount = whCount
                    }
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }





    }
}
