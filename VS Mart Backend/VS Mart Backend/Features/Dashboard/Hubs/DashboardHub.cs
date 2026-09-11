using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dashboard.Hubs
{
    /// <summary>
    /// Unified SignalR Hub for real-time dashboard micro-deltas across all 6 sections:
    /// Live Stock, Cycle Count, Store Validation, DC Encoding, Tag Management, Vendor Discrepancy.
    /// </summary>
    public class DashboardHub : Hub
    {
        // 1. Live Stock
        public async Task BroadcastLiveStockPatch(LiveStockDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveLiveStockPatch", patch);
        }

        // Backward-compatible method name
        public async Task BroadcastPatch(LiveStockDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveLiveStockPatch", patch);
        }

        // 2. Cycle Count
        public async Task BroadcastCycleCountPatch(CycleCountDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveCycleCountPatch", patch);
        }

        // 3. Store Validation
        public async Task BroadcastStoreValidationPatch(StoreValidationDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveStoreValidationPatch", patch);
        }

        // 4. DC Encoding
        public async Task BroadcastDcEncodingPatch(DcEncodingDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveDcEncodingPatch", patch);
        }

        // 5. Tag Management
        public async Task BroadcastTagManagementPatch(TagManagementDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveTagManagementPatch", patch);
        }

        // 6. Vendor Discrepancy
        public async Task BroadcastVendorDiscrepancyPatch(VendorDiscrepancyDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveVendorDiscrepancyPatch", patch);
        }
    }

    /// <summary>
    /// Backward-compatible alias
    /// </summary>
    public class LiveStockHub : DashboardHub
    {
    }
}
