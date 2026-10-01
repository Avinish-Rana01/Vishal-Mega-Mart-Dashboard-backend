namespace VS_Mart_Backend.Features.Reports
{
    public interface ITagCleaningReport
    {
        Task<TagCleaningReportResponse> GetTagCleaningReportAsync(TagCleaningReportRequest request);
        Task<TagCleaningResponse> GetTagCleaningDataAsync(TagCleaningRequest request);


    }
}
