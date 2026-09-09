using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.MainDashboard
{
    public interface IMainDashboardService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);

        Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request);
        Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request);
        Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request);
        Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request);
        Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request);
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request);
        Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request);
        Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request);
        Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request);
        Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request);
        Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request);
    }
}
