using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.CycleCountReport
{
    public interface ICycleCountReportService
    {
        Task<CycleCountReportViewResponse> GetCycleCountReportViewAsync(CycleCountReportViewQueryRequest request);
        Task<CycleCountDetailsResponse> GetCycleCountDetailsAsync(CycleCountDetailsQueryRequest request);
    }
}
