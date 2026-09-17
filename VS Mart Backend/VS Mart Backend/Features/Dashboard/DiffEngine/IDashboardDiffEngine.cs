using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Features.Dashboard.DiffEngine
{
    public interface IDashboardDiffEngine
    {
        Task ProcessLiveStockDiffAsync(LiveStockResponse response, CancellationToken cancellationToken = default);
        Task ProcessCycleCountDiffAsync(CycleCountDashboardResponse response, CancellationToken cancellationToken = default);
        Task ProcessVendorDiscrepancyDiffAsync(VendorHUDiscrepancyResponse response, CancellationToken cancellationToken = default);
        Task ProcessStoreValidationDiffAsync(StoreDashboardResponse response, CancellationToken cancellationToken = default);
        Task ProcessWarehouseEncodingDiffAsync(WarehouseEncodingResponse response, CancellationToken cancellationToken = default);
        Task ProcessTagManagementDiffAsync(TagManagementResponse response, CancellationToken cancellationToken = default);
        Task ProcessDcValidationDiffAsync(DcValidateDashboardResponse response, CancellationToken cancellationToken = default);
    }
}
