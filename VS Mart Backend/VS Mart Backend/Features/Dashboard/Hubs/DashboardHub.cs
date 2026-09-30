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
        private static int _connectedClients = 0;
        public static int ConnectedClientsCount => Math.Max(0, Volatile.Read(ref _connectedClients));

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<string, byte>> _storeSubscriptions = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<int, byte>> _connectionStores = new();

        public static IReadOnlyCollection<int> GetActiveStoreIds()
        {
            return _storeSubscriptions.Where(kvp => !kvp.Value.IsEmpty).Select(kvp => kvp.Key).ToList();
        }

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _connectedClients);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Interlocked.Decrement(ref _connectedClients);

            if (_connectionStores.TryRemove(Context.ConnectionId, out var subscribedStores))
            {
                foreach (var entry in subscribedStores)
                {
                    if (_storeSubscriptions.TryGetValue(entry.Key, out var set))
                    {
                        set.TryRemove(Context.ConnectionId, out _);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeStoreCounters(int storeId)
        {
            if (storeId <= 0) return;
            string groupName = $"store_counters_{storeId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var connectionSet = _storeSubscriptions.GetOrAdd(storeId, _ => new System.Collections.Concurrent.ConcurrentDictionary<string, byte>());
            connectionSet.TryAdd(Context.ConnectionId, 0);

            var storeSet = _connectionStores.GetOrAdd(Context.ConnectionId, _ => new System.Collections.Concurrent.ConcurrentDictionary<int, byte>());
            storeSet.TryAdd(storeId, 0);
        }

        public async Task UnsubscribeStoreCounters(int storeId)
        {
            if (storeId <= 0) return;
            string groupName = $"store_counters_{storeId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (_storeSubscriptions.TryGetValue(storeId, out var connectionSet))
            {
                connectionSet.TryRemove(Context.ConnectionId, out _);
            }

            // Clean up _connectionStores so poller stops querying unwatched stores
            if (_connectionStores.TryGetValue(Context.ConnectionId, out var storeSet))
            {
                storeSet.TryRemove(storeId, out _);
            }
        }

        // 7. Counter Status Patch
        public async Task BroadcastCounterStatusPatch(int storeId, CounterStatusDeltaPatch patch)
        {
            string groupName = $"store_counters_{storeId}";
            await Clients.Group(groupName).SendAsync("ReceiveCounterStatusPatch", patch);
        }

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
