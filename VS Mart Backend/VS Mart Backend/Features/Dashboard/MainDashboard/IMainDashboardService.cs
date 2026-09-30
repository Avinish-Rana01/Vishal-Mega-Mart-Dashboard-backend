using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.MainDashboard
{
    public interface IMainDashboardService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);

        Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request, bool forceRefresh = false);
        Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request, bool forceRefresh = false);
        Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request, bool forceRefresh = false);
        Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request, bool forceRefresh = false);
        Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request, bool forceRefresh = false);
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request, bool forceRefresh = false);
        Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request, bool forceRefresh = false);
        Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request, bool forceRefresh = false);
        Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request, bool forceRefresh = false);
        Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request, bool forceRefresh = false);
        Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request, bool forceRefresh = false);

        Task<int> GetActiveSuperAdminIdAsync();
        Task<VS_Mart_Backend.Features.Base.UserProfileDto> GetUserProfileAsync(string? userId);
    }
}
