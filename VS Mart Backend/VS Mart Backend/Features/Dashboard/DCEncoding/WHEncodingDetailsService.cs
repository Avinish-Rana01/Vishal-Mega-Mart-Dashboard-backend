using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace VS_Mart_Backend.Features.Dashboard.DCEncoding
{
    public class WHEncodingDetailsService : IWHEncodingDetails
    {
        private readonly string _connectionString;
        private readonly ILogger<WHEncodingDetailsService> _logger;

        public WHEncodingDetailsService(IConfiguration configuration, ILogger<WHEncodingDetailsService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }


        public async Task<WHEncodingResponse> GetWHEncodingDetailsAsync(WHEncodingRequest request)
        {
            try
            {
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

                var connectionString = _connectionString;

                using var connection = new SqlConnection(connectionString);

                var parameters = new DynamicParameters();

                // Input parameters
                parameters.Add("@status", "SHOW_WAREHOUSE_ENCODE_DATA", DbType.String);

                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);

                parameters.Add("@PageIndex", request.PageIndex, DbType.Int32);

                parameters.Add("@PageSize", request.PageSize, DbType.Int32);

                parameters.Add("@User_ID", request.User ?? 0, DbType.Int32);

                parameters.Add("@fromdate", fromDate, DbType.Date);

                parameters.Add("@todate", toDate, DbType.Date);

                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "DATE" : request.SortColumn, DbType.String);

                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "desc" : request.SortDirection, DbType.String);


                // Output parameters
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // Hour-wise quantity
                parameters.Add("@8TO9", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@9TO10", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@10TO11", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@11TO12", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@12TO13", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@13TO14", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@14TO15", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@15TO16", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@16TO17", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@17TO18", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@18TO19", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@19TO20", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // Hour-wise errors
                parameters.Add("@8TO9_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@9TO10_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@10TO11_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@11TO12_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@12TO13_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@13TO14_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@14TO15_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@15TO16_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@16TO17_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@17TO18_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@18TO19_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@19TO20_ERR", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // Summary
                parameters.Add("@MRGQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@EVNQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@ENCQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@AVGQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@T_ENC_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@T_ENC_USERS", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@ERRQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);


                // Execute SP
                using var multi = await connection.QueryMultipleAsync("[SP_NEW_REPORT]", parameters, commandType: CommandType.StoredProcedure);

                // First result = warehouse encoding data
                var data = (await multi.ReadAsync<dynamic>()).ToList();


                // Build response
                var response = new WHEncodingResponse
                {
                    Data = data,

                    PageIndex = request.PageIndex,

                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,

                    TotalCount = parameters.Get<int?>("@QTY") ?? 0,

                    UserCount = parameters.Get<int?>("@TotalCount") ?? 0,

                    C8TO9 = parameters.Get<int?>("@8TO9") ?? 0,
                    C9TO10 = parameters.Get<int?>("@9TO10") ?? 0,
                    C10TO11 = parameters.Get<int?>("@10TO11") ?? 0,
                    C11TO12 = parameters.Get<int?>("@11TO12") ?? 0,
                    C12TO13 = parameters.Get<int?>("@12TO13") ?? 0,
                    C13TO14 = parameters.Get<int?>("@13TO14") ?? 0,
                    C14TO15 = parameters.Get<int?>("@14TO15") ?? 0,
                    C15TO16 = parameters.Get<int?>("@15TO16") ?? 0,
                    C16TO17 = parameters.Get<int?>("@16TO17") ?? 0,
                    C17TO18 = parameters.Get<int?>("@17TO18") ?? 0,
                    C18TO19 = parameters.Get<int?>("@18TO19") ?? 0,
                    C19TO20 = parameters.Get<int?>("@19TO20") ?? 0,

                    C8TO9_ERR = parameters.Get<int?>("@8TO9_ERR") ?? 0,
                    C9TO10_ERR = parameters.Get<int?>("@9TO10_ERR") ?? 0,
                    C10TO11_ERR = parameters.Get<int?>("@10TO11_ERR") ?? 0,
                    C11TO12_ERR = parameters.Get<int?>("@11TO12_ERR") ?? 0,
                    C12TO13_ERR = parameters.Get<int?>("@12TO13_ERR") ?? 0,
                    C13TO14_ERR = parameters.Get<int?>("@13TO14_ERR") ?? 0,
                    C14TO15_ERR = parameters.Get<int?>("@14TO15_ERR") ?? 0,
                    C15TO16_ERR = parameters.Get<int?>("@15TO16_ERR") ?? 0,
                    C16TO17_ERR = parameters.Get<int?>("@16TO17_ERR") ?? 0,
                    C17TO18_ERR = parameters.Get<int?>("@17TO18_ERR") ?? 0,
                    C18TO19_ERR = parameters.Get<int?>("@18TO19_ERR") ?? 0,
                    C19TO20_ERR = parameters.Get<int?>("@19TO20_ERR") ?? 0,

                    MRGQTY = parameters.Get<int?>("@MRGQTY") ?? 0,
                    EVNQTY = parameters.Get<int?>("@EVNQTY") ?? 0,
                    ENCQTY = parameters.Get<int?>("@ENCQTY") ?? 0,
                    AVGQTY = parameters.Get<int?>("@AVGQTY") ?? 0,

                    T_ENC_QTY = parameters.Get<int?>("@T_ENC_QTY") ?? 0,
                    T_ENC_USERS = parameters.Get<int?>("@T_ENC_USERS") ?? 0,
                    ERRORQTY = parameters.Get<int?>("@ERRQTY") ?? 0
                };

                return response;
            }
            catch (Exception ex)
            {
                return new WHEncodingResponse();
            }


        }

        public async Task<List<Username>> SearchUsernameAsync(UsernameRequest request)
        {
            try
            {
                string fromDateValue = string.Empty;
                string toDateValue = string.Empty;
                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (!string.IsNullOrWhiteSpace(request.FromDate))
                {
                     fromDateValue = request.FromDate.Trim('"');
                    fromDate = DateTime.ParseExact(fromDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrWhiteSpace(request.ToDate))
                {
                     toDateValue = request.ToDate.Trim('"');
                    toDate = DateTime.ParseExact(toDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();

                // Same logic as old WebForms code
                string status;

                if (string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    status = "BIND_WAREHOUSE_ENCODE_USERS";
                }
                else
                {
                    status = "SEARCH_BIND_WAREHOUSE_ENCODE_USERS";
                }

                parameters.Add("@status", status, DbType.String);

                parameters.Add("@SearchTerm", string.IsNullOrWhiteSpace(request.SearchTerm) ? "" : request.SearchTerm.Trim(), DbType.String);

                parameters.Add("@fromdate", fromDateValue, DbType.Date);

                // Old code:
                //
                // if (toDate == "")
                //     @todate = fromDate
                //
                // Same behavior here.

               // string toDate = string.IsNullOrWhiteSpace(request.ToDate) ? request.FromDate ?? "" : request.ToDate;

                parameters.Add("@todate", toDateValue, DbType.Date);

                // Old code was taking User_ID from Session.
                //
                // In ASP.NET Core, don't use HttpContext.Current.
                // Get it from the authenticated user's claims.

                //int? userId = null;

                //parameters.Add("@User_ID", userId ?? 0, DbType.Int32);

                var result = await connection.QueryAsync<Username>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
            catch (Exception ex)
            {
                return new List<Username>();
            }

        }

    }
}
