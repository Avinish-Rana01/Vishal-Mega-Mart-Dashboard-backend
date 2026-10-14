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
                int userIdVal = 0;
                int.TryParse(request.UserId, out userIdVal);
                parameters.Add("@User_ID", userIdVal, DbType.Int32);

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

                // If requested page extends into inward master tags (VMM_TAG_MST), fill remaining items from VMM_TAG_MST
                int historyCount = 554_376;
                int pageStartRow = (request.PageIndex - 1) * request.PageSize + 1;
                if (data.Count < request.PageSize && (pageStartRow + data.Count) > historyCount)
                {
                    int needed = request.PageSize - data.Count;
                    int vmmOffset = Math.Max(0, (pageStartRow + data.Count) - historyCount - 1);

                    const string vmmQuery = @"
SELECT 
    CAST(ISNULL(MAX(ReadTime), GETDATE()) AS DATE) AS [DATE],
    EPC AS [TID],
    0 AS [CYCLE_COUNT],
    'Warehouse' AS [LOCATION],
    'VMM LUHARI' AS [LOCATION_NAME],
    'New Tag Inward' AS [STATUS]
FROM [dbo].[VMM_TAG_MST]
WHERE (@SearchTerm = '' OR EPC LIKE '%' + @SearchTerm + '%' OR 'Warehouse' LIKE '%' + @SearchTerm + '%' OR 'VMM LUHARI' LIKE '%' + @SearchTerm + '%' OR 'New Tag Inward' LIKE '%' + @SearchTerm + '%')
GROUP BY EPC
ORDER BY EPC
OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

                    var vmmRows = await connection.QueryAsync<TagDetailsData>(
                        vmmQuery,
                        new { SearchTerm = request.SearchTerm ?? "", Offset = vmmOffset, Limit = needed },
                        commandTimeout: 120);

                    long currentNum = pageStartRow + data.Count;
                    foreach (var row in vmmRows)
                    {
                        row.RowNumber = currentNum++;
                        data.Add(row);
                    }
                }

                int calculatedWhCount = (storeCount > 0 && recordCount > storeCount)
                    ? (recordCount - storeCount)
                    : whCount;

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
                        WhCount = calculatedWhCount
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }





    }
}
