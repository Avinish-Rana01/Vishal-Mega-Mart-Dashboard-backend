using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.SaleDashboard
{
    public interface ISaleDashboardService
    {
        Task<StoreSaleReportResponse> GetStoreSaleReportAsync(StoreSaleReportQueryRequest request);
        Task<List<DropdownItem>> BindPOSCounterAsync(BindPOSCounterRequest request);
        Task<List<DropdownItem>> SearchArticlesSaleAsync(SearchArticlesSaleRequest request);
        Task<List<DropdownItem>> SearchEANSaleAsync(SearchEANSaleRequest request);
        Task<SaleDataResponse> GetSaleDataAsync(SaleDataQueryRequest request);
    }
}
