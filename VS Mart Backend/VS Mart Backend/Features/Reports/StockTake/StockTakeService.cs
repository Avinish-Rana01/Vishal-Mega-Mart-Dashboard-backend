using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace VS_Mart_Backend.Features.Reports.StockTake
{
    public class StockTakeService : IStockTake
    {
        private readonly string _connectionString;
        private readonly ILogger<StockTakeService> _logger;

        public StockTakeService(IConfiguration configuration, ILogger<StockTakeService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }


        public async Task<StockTakeResponse> GetStockTakeDataAsync(StockTakeRequest request, int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (!string.IsNullOrWhiteSpace(request.FromDate))
                {
                    string fromDateValue = request.FromDate.Trim('"');
                    fromDate = DateTime.ParseExact(fromDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrWhiteSpace(request.ToDate))
                {
                    string toDateValue = request.ToDate.Trim('"');
                    toDate = DateTime.ParseExact(toDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }


                var parameters = new DynamicParameters();

                parameters.Add("@status", "VIEW_STOCK_TAKE_REPORT", DbType.String);

                parameters.Add("@USER_ID", userId, DbType.Int32);

                parameters.Add("@Store_code", request.StoreCode ?? "", DbType.String);

                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);

                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);

                parameters.Add("@PageSize", request.PageSize, DbType.Int32);

                parameters.Add("@fromdate", fromDate, DbType.Date);

                parameters.Add("@todate", toDate, DbType.Date);

                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "REF_NO" : request.SortColumn, DbType.String);

                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String);

                // Output parameters
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@SCANQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@Excess_Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // Execute stored procedure
                var data = await connection.QueryAsync("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure);


                // Read output parameters
                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;

                int totalCount = parameters.Get<int?>("@TotalCount") ?? 0;

                int actualQty = parameters.Get<int?>("@ACTUALQTY") ?? 0;

                int scannedQty = parameters.Get<int?>("@SCANQTY") ?? 0;

                int excessQty = parameters.Get<int?>("@Excess_Qty") ?? 0;

                int differenceQty = parameters.Get<int?>("@DIFFQTY") ?? 0;


                return new StockTakeResponse
                {
                    Data = data,

                    PageIndex = request.PageIndex,
                    RecordCount = recordCount,
                    TotalCount = totalCount,

                    ActualQty = actualQty,
                    ScannedQty = scannedQty,
                    DifferenceQty = differenceQty,
                    ExcessQty = excessQty
                };
            }
            catch (Exception ex)
            {
                return new StockTakeResponse();
            }

        }

    }
}
