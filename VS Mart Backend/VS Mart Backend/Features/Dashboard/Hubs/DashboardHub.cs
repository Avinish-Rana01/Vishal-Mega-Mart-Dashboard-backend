using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.MainDashboard;
using VS_Mart_Backend.Features.Base;

namespace VS_Mart_Backend.Features.Dashboard.Hubs
{
    /// <summary>
    /// Unified SignalR Hub for real-time dashboard micro-deltas across all 6 sections:
    /// Live Stock, Cycle Count, Store Validation, DC Encoding, Tag Management, Vendor Discrepancy.
    /// Supports role-based and store-partitioned group routing.
    /// </summary>
    public class DashboardHub : Hub
    {
        private static int _connectedClients = 0;
        public static int ConnectedClientsCount => Math.Max(0, Volatile.Read(ref _connectedClients));

        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _storeSubscriptions = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> _connectionStores = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connectionDashboardGroups = new();
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _userConnections = new();
        private static readonly ConcurrentDictionary<string, int> _connectionUserMap = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userKnownStores = new();

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardHub> _logger;

        public DashboardHub(IServiceProvider serviceProvider, ILogger<DashboardHub> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public static IReadOnlyCollection<int> GetActiveStoreIds()
        {
            return _storeSubscriptions.Where(kvp => !kvp.Value.IsEmpty).Select(kvp => kvp.Key).ToList();
        }

        public static IReadOnlyCollection<int> GetConnectedUserIds()
        {
            return _userConnections.Where(kvp => !kvp.Value.IsEmpty).Select(kvp => kvp.Key).ToList();
        }

        public static HashSet<string>? GetUserKnownStores(int userId)
        {
            if (_userKnownStores.TryGetValue(userId, out var stores))
            {
                lock (stores)
                {
                    return new HashSet<string>(stores, StringComparer.OrdinalIgnoreCase);
                }
            }
            return null;
        }

        public static void SetUserKnownStores(int userId, IEnumerable<string> stores)
        {
            var set = new HashSet<string>(stores.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
            _userKnownStores[userId] = set;
        }

        public static async Task UpdateUserStoreGroupsAsync(
            IHubContext<DashboardHub> hubContext,
            int userId,
            IEnumerable<string> addedStores,
            IEnumerable<string> removedStores)
        {
            if (_userConnections.TryGetValue(userId, out var conns) && !conns.IsEmpty)
            {
                foreach (var connId in conns.Keys)
                {
                    var groups = _connectionDashboardGroups.GetOrAdd(connId, _ => new(StringComparer.OrdinalIgnoreCase));

                    foreach (var store in removedStores)
                    {
                        if (!string.IsNullOrWhiteSpace(store))
                        {
                            string groupName = $"store_{store.Trim().ToUpperInvariant()}";
                            try { await hubContext.Groups.RemoveFromGroupAsync(connId, groupName); } catch { }
                            groups.TryRemove(groupName, out _);
                        }
                    }

                    foreach (var store in addedStores)
                    {
                        if (!string.IsNullOrWhiteSpace(store))
                        {
                            string groupName = $"store_{store.Trim().ToUpperInvariant()}";
                            try { await hubContext.Groups.AddToGroupAsync(connId, groupName); } catch { }
                            groups.TryAdd(groupName, 0);
                        }
                    }
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _connectedClients);

            var httpContext = Context.GetHttpContext();
            string? userId = httpContext?.Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await SubscribeDashboard(userId);
            }

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

            if (_connectionUserMap.TryRemove(Context.ConnectionId, out int disconnectedUid))
            {
                if (_userConnections.TryGetValue(disconnectedUid, out var conns))
                {
                    conns.TryRemove(Context.ConnectionId, out _);
                    if (conns.IsEmpty)
                    {
                        _userConnections.TryRemove(disconnectedUid, out _);
                        _userKnownStores.TryRemove(disconnectedUid, out _);
                    }
                }
            }

            _connectionDashboardGroups.TryRemove(Context.ConnectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribes the connecting user to role-scoped dashboard groups.
        /// Super Admins join 'role_super_admin' (receives all stores).
        /// Other roles join 'store_{STORECODE}' for each authorized store.
        /// </summary>
        public async Task SubscribeDashboard(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out int uid) || uid <= 0)
            {
                return;
            }

            try
            {
                _userConnections.GetOrAdd(uid, _ => new ConcurrentDictionary<string, byte>()).TryAdd(Context.ConnectionId, 0);
                _connectionUserMap[Context.ConnectionId] = uid;

                // Clean up any existing dashboard groups for this connection if re-subscribing
                if (_connectionDashboardGroups.TryGetValue(Context.ConnectionId, out var existingGroups))
                {
                    foreach (var oldGroup in existingGroups.Keys)
                    {
                        try { await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldGroup); } catch { }
                    }
                    existingGroups.Clear();
                }

                var currentGroups = _connectionDashboardGroups.GetOrAdd(Context.ConnectionId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));

                using var scope = _serviceProvider.CreateScope();
                var dashboardService = scope.ServiceProvider.GetRequiredService<IMainDashboardService>();
                var profile = await dashboardService.GetUserProfileAsync(userId);
                if (profile == null) return;

                // Add to private user group
                string userGroup = $"user_{uid}";
                await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);
                currentGroups.TryAdd(userGroup, 0);

                if (profile.IsSuperAdmin)
                {
                    string superGroup = "role_super_admin";
                    await Groups.AddToGroupAsync(Context.ConnectionId, superGroup);
                    currentGroups.TryAdd(superGroup, 0);

                    _logger.LogInformation("DashboardHub: Connection {ConnId} (User {UserId}) subscribed as Super Admin.", Context.ConnectionId, userId);
                }
                else
                {
                    var storeCodes = (profile.AuthorizedStoreCodes ?? new List<string>())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim().ToUpperInvariant())
                        .Distinct()
                        .ToList();

                    // Baseline the user's currently authorized stores
                    SetUserKnownStores(uid, storeCodes);

                    foreach (var store in storeCodes)
                    {
                        string storeGroup = $"store_{store}";
                        await Groups.AddToGroupAsync(Context.ConnectionId, storeGroup);
                        currentGroups.TryAdd(storeGroup, 0);
                    }

                    _logger.LogInformation("DashboardHub: Connection {ConnId} (User {UserId}, {UserType}) subscribed to {Count} store group(s): {Stores}", 
                        Context.ConnectionId, userId, profile.UserType, storeCodes.Count, string.Join(", ", storeCodes));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardHub: Error subscribing dashboard for connection {ConnId}, user {UserId}", Context.ConnectionId, userId);
            }
        }

        public async Task SubscribeStoreCounters(int storeId)
        {
            if (storeId <= 0) return;
            string groupName = $"store_counters_{storeId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var connectionSet = _storeSubscriptions.GetOrAdd(storeId, _ => new ConcurrentDictionary<string, byte>());
            connectionSet.TryAdd(Context.ConnectionId, 0);

            var storeSet = _connectionStores.GetOrAdd(Context.ConnectionId, _ => new ConcurrentDictionary<int, byte>());
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
            if (patch != null && !string.IsNullOrEmpty(patch.StoreCode))
            {
                string groupName = $"store_{patch.StoreCode.Trim().ToUpperInvariant()}";
                await Clients.Groups("role_super_admin", groupName).SendAsync("ReceiveLiveStockPatch", patch);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveLiveStockPatch", patch);
            }
        }

        // Backward-compatible method name
        public async Task BroadcastPatch(LiveStockDeltaPatch patch)
        {
            await BroadcastLiveStockPatch(patch);
        }

        // 2. Cycle Count
        public async Task BroadcastCycleCountPatch(CycleCountDeltaPatch patch)
        {
            if (patch != null && !string.IsNullOrEmpty(patch.StoreCode))
            {
                string groupName = $"store_{patch.StoreCode.Trim().ToUpperInvariant()}";
                await Clients.Groups("role_super_admin", groupName).SendAsync("ReceiveCycleCountPatch", patch);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveCycleCountPatch", patch);
            }
        }

        // 3. Store Validation
        public async Task BroadcastStoreValidationPatch(StoreValidationDeltaPatch patch)
        {
            if (patch != null && !string.IsNullOrEmpty(patch.StoreCode))
            {
                string groupName = $"store_{patch.StoreCode.Trim().ToUpperInvariant()}";
                await Clients.Groups("role_super_admin", groupName).SendAsync("ReceiveStoreValidationPatch", patch);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveStoreValidationPatch", patch);
            }
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

        // 7. DC Validation
        public async Task BroadcastDcValidationPatch(DcValidationDeltaPatch patch)
        {
            if (patch != null && !string.IsNullOrEmpty(patch.RecivingPlant))
            {
                string groupName = $"store_{patch.RecivingPlant.Trim().ToUpperInvariant()}";
                await Clients.Groups("role_super_admin", groupName).SendAsync("ReceiveDcValidationPatch", patch);
            }
            else
            {
                await Clients.All.SendAsync("ReceiveDcValidationPatch", patch);
            }
        }
    }

    /// <summary>
    /// Backward-compatible alias
    /// </summary>
    public class LiveStockHub : DashboardHub
    {
        public LiveStockHub(IServiceProvider serviceProvider, ILogger<DashboardHub> logger) 
            : base(serviceProvider, logger)
        {
        }
    }
}
