using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Services
{
    /// <summary>
    /// Background service responsible for pre-warming in-memory dashboard caches
    /// and running periodic difference checks across active store modules.
    /// </summary>
    public class CacheWarmerService : BackgroundService
    {
        public static int TotalRuns { get; private set; } = 0;
        public static DateTime? LastRunTime { get; private set; }
        public static string LastStatus { get; private set; } = "Initializing";
        public static DateTime LastPeriodicWarmupTime { get; private set; } = DateTime.MinValue;
        public static TimeSpan PeriodicInterval { get; private set; } = TimeSpan.FromMinutes(60);
        public static TimeSpan RealtimeWarmerInterval { get; private set; } = TimeSpan.FromSeconds(10);

        private static readonly System.Threading.SemaphoreSlim _wakeUpSignal = new System.Threading.SemaphoreSlim(0, 1);

        public static void TriggerImmediateWarmup()
        {
            if (_wakeUpSignal.CurrentCount == 0)
            {
                try { _wakeUpSignal.Release(); } catch { }
            }
        }

        private readonly ILogger<CacheWarmerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly VS_Mart_Backend.Features.Dashboard.DiffEngine.IDashboardDiffEngine _diffEngine;
        private readonly TimeSpan _periodicInterval;
        private readonly TimeSpan _realtimeDelay;
        private DateTime _lastPeriodicWarmupTime = DateTime.MinValue;

        public CacheWarmerService(
            ILogger<CacheWarmerService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            VS_Mart_Backend.Features.Dashboard.DiffEngine.IDashboardDiffEngine diffEngine)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _diffEngine = diffEngine;

            int refreshMinutes = _configuration.GetValue<int>("DashboardSettings:SlowMovingDashboardRefreshMinutes", 60);
            _periodicInterval = TimeSpan.FromMinutes(refreshMinutes > 0 ? refreshMinutes : 60);
            PeriodicInterval = _periodicInterval;

            int realtimeSeconds = _configuration.GetValue<int>("DashboardSettings:RealtimeWarmerIntervalSeconds", 10);
            _realtimeDelay = TimeSpan.FromSeconds(realtimeSeconds > 0 ? realtimeSeconds : 10);
            RealtimeWarmerInterval = _realtimeDelay;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheWarmerService started with DashboardDiffEngine.");

            var dashboardTask = RunDashboardWarmerLoopAsync(stoppingToken);
            var counterTask = RunActiveCounterPollerLoopAsync(stoppingToken);

            await Task.WhenAll(dashboardTask, counterTask);
        }

        private async Task RunDashboardWarmerLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LastRunTime = DateTime.Now;
                LastStatus = "Running warmup pass";
                _logger.LogInformation("CacheWarmerService running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var liveStockService = scope.ServiceProvider.GetRequiredService<IMainDashboardService>();

                        if (!liveStockService.IsCacheEnabled())
                        {
                            _logger.LogInformation("Cache is currently DISABLED. Skipping pre-warming iteration.");
                            await _wakeUpSignal.WaitAsync(_realtimeDelay, stoppingToken);
                            continue;
                        }

                        // Dynamically discover any active Super Admin (self-healing, zero hardcoding)
                        int superAdminId = await liveStockService.GetActiveSuperAdminIdAsync();
                        string superAdminIdStr = superAdminId.ToString();

                        // 1. LiveStock
                        var liveStockRequest = new LiveStockQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000 };
                        var liveStockData = await liveStockService.GetLiveStockDetailsAsync(liveStockRequest, forceRefresh: true);
                        await _diffEngine.ProcessLiveStockDiffAsync(liveStockData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 2. Cycle Count
                        var cycleCountRequest = new CycleCountDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000, SortColumn = "STORE CODE", SortDirection = "ASC" };
                        var cycleCountData = await liveStockService.GetCycleCountDashboardAsync(cycleCountRequest, forceRefresh: true);
                        await _diffEngine.ProcessCycleCountDiffAsync(cycleCountData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 3. Vendor HU Discrepancy
                        var vendorHuRequest = new VendorHUDiscrepancyQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000, SortColumn = "DIFF_TILL_DATE", SortDirection = "asc" };
                        var vendorHuData = await liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest, forceRefresh: true);
                        await _diffEngine.ProcessVendorDiscrepancyDiffAsync(vendorHuData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 4. Tag Management (Real-time Location Distribution)
                        var tagRequest = new TagManagementQueryRequest();
                        var tagData = await liveStockService.GetTagManagementDataAsync(tagRequest, forceRefresh: true);
                        await _diffEngine.ProcessTagManagementDiffAsync(tagData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 5. Warehouse Encoding
                        var encodeRequest = new WarehouseEncodingQueryRequest { FromDate = DateTime.Now.ToString("yyyy-MM-dd"), ToDate = DateTime.Now.ToString("yyyy-MM-dd") };
                        var encodeData = await liveStockService.GetWarehouseEncodingDataAsync(encodeRequest, forceRefresh: true);
                        await _diffEngine.ProcessWarehouseEncodingDiffAsync(encodeData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 6. Store Validation / Dashboard
                        var storeDashboardRequest = new StoreDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000, SortColumn = "Store", SortDirection = "asc" };
                        var storeDashboardData = await liveStockService.GetStoreDashboardAsync(storeDashboardRequest, forceRefresh: true);
                        await _diffEngine.ProcessStoreValidationDiffAsync(storeDashboardData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 7. DC Validation
                        var dcValidateRequest = new DcValidateDashboardQueryRequest { UserId = superAdminIdStr, PageIndex = 1, PageSize = 1000 };
                        var dcValidateData = await liveStockService.GetDcValidateDashboardAsync(dcValidateRequest, forceRefresh: true);
                        await _diffEngine.ProcessDcValidationDiffAsync(dcValidateData, stoppingToken);
                        await Task.Delay(150, stoppingToken);

                        // 8, 9, 10, 11: Periodic Dashboards (Sale, Void, Return, Tag Cycle Count - refreshed every _periodicInterval, e.g. 60 mins)
                        bool shouldRunPeriodicWarmup = (DateTime.Now - _lastPeriodicWarmupTime) >= _periodicInterval;

                        if (shouldRunPeriodicWarmup)
                        {
                            _logger.LogInformation("CacheWarmerService: Performing periodic refresh for Sale, Void, Return, and Tag Cycle Count (Interval: {mins} mins).", _periodicInterval.TotalMinutes);

                            // 8. Sale Dashboard
                            var saleDashboardRequest = new SaleDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000 };
                            await liveStockService.GetSaleDashboardAsync(saleDashboardRequest, forceRefresh: true);
                            await Task.Delay(150, stoppingToken);

                            // 9. Void Dashboard
                            var voidDashboardRequest = new VoidDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000 };
                            await liveStockService.GetVoidDashboardAsync(voidDashboardRequest, forceRefresh: true);
                            await Task.Delay(150, stoppingToken);

                            // 10. Return Dashboard
                            var returnDashboardRequest = new ReturnDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 1000 };
                            await liveStockService.GetReturnDashboardAsync(returnDashboardRequest, forceRefresh: true);
                            await Task.Delay(150, stoppingToken);

                            // 11. Tag Cycle Count (Slow-moving lifetime recycling statistics)
                            var tagCycleCountRequest = new TagCycleCountQueryRequest { SearchTerm = "", PageIndex = 1, PageSize = 1000, SortColumn = "CYCLE_COUNT", SortDirection = "DESC" };
                            await liveStockService.GetTagCycleCountDataAsync(tagCycleCountRequest, forceRefresh: true);
                            await Task.Delay(150, stoppingToken);

                            _lastPeriodicWarmupTime = DateTime.Now;
                            LastPeriodicWarmupTime = _lastPeriodicWarmupTime;
                        }

                        TotalRuns++;
                        LastRunTime = DateTime.Now;
                        LastStatus = $"Pre-warmed successfully (Iteration #{TotalRuns})";
                        _logger.LogInformation("Cache pre-warmed & diff checked successfully. Iteration #{runs}", TotalRuns);
                    }
                }
                catch (Exception ex)
                {
                    LastStatus = $"Error: {ex.Message}";
                    _logger.LogError(ex, "Error occurred while pre-warming the cache.");
                }

                await _wakeUpSignal.WaitAsync(_realtimeDelay, stoppingToken);
            }

            _logger.LogInformation("CacheWarmerService dashboard loop is stopping.");
        }

        private async Task RunActiveCounterPollerLoopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheWarmerService: Active Counter Poller loop started (Interval: 3s, Mode: Parallel).");
            var pollInterval = TimeSpan.FromSeconds(3.0);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeStoreIds = VS_Mart_Backend.Features.Dashboard.Hubs.DashboardHub.GetActiveStoreIds();
                    if (activeStoreIds.Count > 0)
                    {
                        // Run all active store queries concurrently.
                        // Each store gets its OWN DI scope — required for scoped services (DbContext, etc.)
                        // No shared state between tasks, so no race conditions.
                        var tasks = activeStoreIds.Select(async storeId =>
                        {
                            try
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var storeService = scope.ServiceProvider.GetRequiredService<VS_Mart_Backend.Features.Store.IStoreService>();

                                var result = await storeService.GetCounterStatusDetailsAsync(storeId);
                                if (result?.Data != null)
                                {
                                    await _diffEngine.ProcessCounterStatusDiffAsync(storeId, result.Data, stoppingToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "CacheWarmerService: Error checking counter status for active store {StoreId}", storeId);
                            }
                        });

                        // Wait for all stores to finish — total time = slowest single query (not sum)
                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CacheWarmerService: Error in active counter poller loop.");
                }

                try
                {
                    await Task.Delay(pollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("CacheWarmerService active counter poller loop is stopping.");
        }
    }
}
