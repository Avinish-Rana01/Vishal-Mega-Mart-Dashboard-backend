using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.LiveStockReport
{
    public interface ILiveStockReportService
    {
        Task<List<StoreDropdownItem>> GetStoresAsync(string? userId);
        Task<List<ArticleItem>> SearchArticlesAsync(ArticleSearchRequest request);
        Task<LiveStockReportResponse> GetLiveStockDetailsAsync(LiveStockReportRequest request);
    }
}
