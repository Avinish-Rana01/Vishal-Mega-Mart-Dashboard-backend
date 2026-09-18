using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using VS_Mart_Backend.Features.Master;

namespace VS_Mart_Backend.Features.Reports
{
    public class TagCleaningReportsService : ITagCleaningReport
    {
        private readonly string _connectionString;
        private readonly ILogger<TagCleaningReportsService> _logger;

        public TagCleaningReportsService(IConfiguration configuration, ILogger<TagCleaningReportsService> logger)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
        }


        public async Task<TagCleaningReportResponse> GetTagCleaningReportAsync(TagCleaningReportRequest request)
        {
            try
            {
                string connectionString = _connectionString;

                using var connection = new SqlConnection(connectionString);

                int startRow = ((request.PageIndex - 1) * request.PageSize) + 1;

                int endRow = request.PageIndex * request.PageSize;

                var parameters = new DynamicParameters();

                parameters.Add("@status", "TAG_CLEANING_REPORT", DbType.String);

                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);

                parameters.Add("@fromdate", request.FromDate ?? "", DbType.String);

                parameters.Add("@todate", request.ToDate ?? "", DbType.String);

                //parameters.Add("@StartRow", startRow, DbType.Int32);

                //parameters.Add("@EndRow", endRow, DbType.Int32);

                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "INWARD_DATE" : request.SortColumn, DbType.String);

                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "DESC" : request.SortDirection, DbType.String);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                parameters.Add("@TAG_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var data = await connection.QueryAsync<TagCleaningReportModel>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);

                return new TagCleaningReportResponse
                {
                    Action = "TAG_CLEANING_REPORT",

                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,

                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,

                    TagValidatedCount = parameters.Get<int?>("@TAG_VALIDATED_QTY") ?? 0,

                    Data = data.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while getting Tag Cleaning Report");

                return new TagCleaningReportResponse();
            }
        }


        //public async Task<List<TagCleaningReportModel>> GetTagCleaningExportDataAsync(TagCleaningReportRequest request)
        //{
        //    try
        //    {
        //        string connectionString = _connectionString;

        //        using var connection = new SqlConnection(connectionString);

        //        var parameters = new DynamicParameters();

        //        parameters.Add("@status", "EXPORT_TAG_CLEANING_REPORT", DbType.String);

        //        parameters.Add("@fromdate", request.FromDate ?? "", DbType.String);

        //        parameters.Add("@todate", request.ToDate ?? "", DbType.String);

        //        parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);

        //        parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        //        var data = await connection.QueryAsync<TagCleaningReportModel>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);

        //        return data.ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while getting Tag Cleaning Export Data");

        //        throw;
        //    }
        //}


    }
}
