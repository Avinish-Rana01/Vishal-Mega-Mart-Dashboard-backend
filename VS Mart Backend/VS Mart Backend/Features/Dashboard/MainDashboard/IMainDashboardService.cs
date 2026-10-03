using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.MainDashboard
{
    /// <summary>
    /// Contract defining main dashboard analytics, real-time stock queries, and user profile retrieval services.
    /// </summary>
    public interface IMainDashboardService
    {
        /// <summary>
        /// Gets the current in-memory cache toggle state.
        /// </summary>
        bool IsCacheEnabled();

        /// <summary>
        /// Sets the in-memory cache toggle state.
        /// </summary>
        void SetCacheEnabled(bool enabled);

        /// <summary>
        /// Retrieves live stock inventory metrics with optional cache bypass.
        /// </summary>
        Task<LiveStockResponse> GetLiveStockDetailsAsync(LiveStockQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves tag cycle count metrics and discrepancy aggregations.
        /// </summary>
        Task<TagCycleCountResponse> GetTagCycleCountDataAsync(TagCycleCountQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves store-level KPI and performance dashboard aggregations.
        /// </summary>
        Task<StoreDashboardResponse> GetStoreDashboardAsync(StoreDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves sales dashboard summary metrics across retail locations.
        /// </summary>
        Task<SaleDashboardResponse> GetSaleDashboardAsync(SaleDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves return transaction metrics and statistics.
        /// </summary>
        Task<ReturnDashboardResponse> GetReturnDashboardAsync(ReturnDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves void transaction metrics and discrepancy trends.
        /// </summary>
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves distribution center validation analytics.
        /// </summary>
        Task<DcValidateDashboardResponse> GetDcValidateDashboardAsync(DcValidateDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves cycle count validation dashboard figures.
        /// </summary>
        Task<CycleCountDashboardResponse> GetCycleCountDashboardAsync(CycleCountDashboardQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves vendor handling unit discrepancy reports.
        /// </summary>
        Task<VendorHUDiscrepancyResponse> GetVendorHUDiscrepancyAsync(VendorHUDiscrepancyQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves RFID tag management and provisioning statistics.
        /// </summary>
        Task<TagManagementResponse> GetTagManagementDataAsync(TagManagementQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Retrieves warehouse encoding operations analytics.
        /// </summary>
        Task<WarehouseEncodingResponse> GetWarehouseEncodingDataAsync(WarehouseEncodingQueryRequest request, bool forceRefresh = false);

        /// <summary>
        /// Resolves the fallback SuperAdmin user identifier.
        /// </summary>
        Task<int> GetActiveSuperAdminIdAsync();

        /// <summary>
        /// Resolves user profile details given an optional user identifier.
        /// </summary>
        Task<VS_Mart_Backend.Features.Base.UserProfileDto> GetUserProfileAsync(string? userId);
    }
}
