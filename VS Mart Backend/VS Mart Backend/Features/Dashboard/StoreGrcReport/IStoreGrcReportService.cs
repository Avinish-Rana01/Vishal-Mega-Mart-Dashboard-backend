using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.StoreGrcReport
{
    public interface IStoreGrcReportService
    {
        Task<List<HuNumberItem>> SearchHuNumbersAsync(GrcHuSearchRequest request);
        Task<GrcDetailsResponse> GetGrcDetailsAsync(GrcDetailsRequest request);
        Task<GrcModalDetailsResponse> GetGrcModalDetailsAsync(GrcModalDetailsRequest request);
        Task<VS_Mart_Backend.Features.MainDashboard.StoreDashboardResponse> GetStoreGrcReportAsync(StoreGrcReportQueryRequest request);
        Task<HUDetailsResponse> GetHUDetailsAsync(HUDetailsRequest request);
    }
}
