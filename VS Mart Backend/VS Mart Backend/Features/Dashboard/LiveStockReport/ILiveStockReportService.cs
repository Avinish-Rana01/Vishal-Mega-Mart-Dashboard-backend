using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.LiveStockReport
{
    public interface ILiveStockReportService
    {
        Task<List<StoreDropdownItem>> GetStoresAsync(string? userId, CancellationToken cancellationToken = default);
        Task<List<ArticleItem>> SearchArticlesAsync(ArticleSearchRequest request, CancellationToken cancellationToken = default);
        Task<LiveStockReportResponse> GetLiveStockDetailsAsync(LiveStockReportRequest request, CancellationToken cancellationToken = default);
    }
}
