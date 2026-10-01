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
                using var connection = new SqlConnection(_connectionString);

                int pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
                int pageSize = request.PageSize < 1 ? 10 : request.PageSize;

                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (!string.IsNullOrWhiteSpace(request.FromDate))
                {
                    string fromVal = request.FromDate.Trim('"').Trim();
                    if (DateTime.TryParse(fromVal, out var parsedFrom))
                        fromDate = parsedFrom;
                }

                if (!string.IsNullOrWhiteSpace(request.ToDate))
                {
                    string toVal = request.ToDate.Trim('"').Trim();
                    if (DateTime.TryParse(toVal, out var parsedTo))
                        toDate = parsedTo;
                }

                var parameters = new DynamicParameters();

                parameters.Add("@status", "TAG_CLEANING_CONSOLIDATE_REPORT", DbType.String);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);
                parameters.Add("@fromdate", fromDate, DbType.Date);
                parameters.Add("@todate", toDate, DbType.Date);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);
                parameters.Add("@SortColumn", string.IsNullOrEmpty(request.SortColumn) ? "TAG_CLEANED_DATE" : request.SortColumn, DbType.String);
                parameters.Add("@SortDirection", string.IsNullOrEmpty(request.SortDirection) ? "DESC" : request.SortDirection, DbType.String);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TAG_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var data = await connection.QueryAsync<TagCleaningReportModel>("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);

                return new TagCleaningReportResponse
                {
                    Action = "TAG_CLEANING_CONSOLIDATE_REPORT",
                    RecordCount = parameters.Get<int?>("@RecordCount") ?? 0,
                    TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    TotalValidatedCount = parameters.Get<int?>("@TAG_VALIDATED_QTY") ?? 0,
                    TotalCleanedCount = parameters.Get<int?>("@TotalCount") ?? 0,
                    Data = data.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting Tag Cleaning Report: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<TagCleaningResponse> GetTagCleaningDataAsync(TagCleaningRequest request)
        {
            try
            {
                request ??= new TagCleaningRequest();

                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (!string.IsNullOrWhiteSpace(request.FromDate))
                {
                    string fromVal = request.FromDate.Trim('"').Trim();
                    if (DateTime.TryParse(fromVal, out var parsedFrom))
                        fromDate = parsedFrom;
                }

                if (!string.IsNullOrWhiteSpace(request.ToDate))
                {
                    string toVal = request.ToDate.Trim('"').Trim();
                    if (DateTime.TryParse(toVal, out var parsedTo))
                        toDate = parsedTo;
                }

                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();

                parameters.Add("@status", "TAG_CLEANING_REPORT", DbType.String);
                parameters.Add("@SearchTerm", request.SearchTerm ?? "", DbType.String);
                parameters.Add("@PageIndex", request.PageIndex <= 0 ? 1 : request.PageIndex, DbType.Int32);
                parameters.Add("@PageSize", request.PageSize <= 0 ? 10 : request.PageSize, DbType.Int32);
                parameters.Add("@fromdate", fromDate, DbType.Date);
                parameters.Add("@todate", toDate, DbType.Date);
                parameters.Add("@SortColumn", string.IsNullOrWhiteSpace(request.SortColumn) ? "INWARD_DATE" : request.SortColumn, DbType.String);
                parameters.Add("@SortDirection", string.IsNullOrWhiteSpace(request.SortDirection) ? "desc" : request.SortDirection, DbType.String);

                // Output parameters
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TAG_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using var multi = await connection.QueryMultipleAsync("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure);

                var data = (await multi.ReadAsync<TagCleaningData>()).ToList();

                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                int totalCount = parameters.Get<int?>("@TotalCount") ?? 0;
                int tagValidatedCount = parameters.Get<int?>("@TAG_VALIDATED_QTY") ?? 0;

                return new TagCleaningResponse
                {
                    Data = data,
                    Pager = new TagCleaningPager
                    {
                        PageIndex = request.PageIndex,
                        RecordCount = recordCount,
                        TotalCount = totalCount,
                        TagValidatedCount = tagValidatedCount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting Tag Cleaning Data: {Message}", ex.Message);
                return new TagCleaningResponse();
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
