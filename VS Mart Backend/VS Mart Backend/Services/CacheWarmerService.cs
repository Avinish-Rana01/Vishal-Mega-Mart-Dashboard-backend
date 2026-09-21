using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Services
{
    public class CacheWarmerService : BackgroundService
    {
        public static int TotalRuns { get; private set; } = 0;
        public static DateTime? LastRunTime { get; private set; }
        public static string LastStatus { get; private set; } = "Initializing";

        private readonly ILogger<CacheWarmerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly VS_Mart_Backend.Features.Dashboard.DiffEngine.IDashboardDiffEngine _diffEngine;

        public CacheWarmerService(
            ILogger<CacheWarmerService> logger,
            IServiceProvider serviceProvider,
            VS_Mart_Backend.Features.Dashboard.DiffEngine.IDashboardDiffEngine diffEngine)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _diffEngine = diffEngine;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheWarmerService started with DashboardDiffEngine.");

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
                            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                            continue;
                        }

                        // Dynamically discover any active Super Admin (self-healing, zero hardcoding)
                        int superAdminId = await liveStockService.GetActiveSuperAdminIdAsync();
                        string superAdminIdStr = superAdminId.ToString();

                        // 1. LiveStock
                        var liveStockRequest = new LiveStockQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        var liveStockData = await liveStockService.GetLiveStockDetailsAsync(liveStockRequest);
                        await _diffEngine.ProcessLiveStockDiffAsync(liveStockData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 2. Cycle Count
                        var cycleCountRequest = new CycleCountDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "STORE CODE", SortDirection = "ASC" };
                        var cycleCountData = await liveStockService.GetCycleCountDashboardAsync(cycleCountRequest);
                        await _diffEngine.ProcessCycleCountDiffAsync(cycleCountData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 3. Vendor HU Discrepancy
                        var vendorHuRequest = new VendorHUDiscrepancyQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "DIFF_TILL_DATE", SortDirection = "asc" };
                        var vendorHuData = await liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest);
                        await _diffEngine.ProcessVendorDiscrepancyDiffAsync(vendorHuData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 4. Tag Management
                        var tagRequest = new TagManagementQueryRequest();
                        var tagData = await liveStockService.GetTagManagementDataAsync(tagRequest);
                        await _diffEngine.ProcessTagManagementDiffAsync(tagData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 5. Warehouse Encoding
                        var encodeRequest = new WarehouseEncodingQueryRequest { FromDate = DateTime.Now.ToString("yyyy-MM-dd"), ToDate = DateTime.Now.ToString("yyyy-MM-dd") };
                        var encodeData = await liveStockService.GetWarehouseEncodingDataAsync(encodeRequest, forceRefresh: true);
                        await _diffEngine.ProcessWarehouseEncodingDiffAsync(encodeData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 6. Store Validation / Dashboard
                        var storeDashboardRequest = new StoreDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "Store", SortDirection = "asc" };
                        var storeDashboardData = await liveStockService.GetStoreDashboardAsync(storeDashboardRequest);
                        await _diffEngine.ProcessStoreValidationDiffAsync(storeDashboardData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 7. Sale Dashboard
                        var saleDashboardRequest = new SaleDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetSaleDashboardAsync(saleDashboardRequest);
                        await Task.Delay(500, stoppingToken);

                        // 8. Void Dashboard
                        var voidDashboardRequest = new VoidDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetVoidDashboardAsync(voidDashboardRequest);
                        await Task.Delay(500, stoppingToken);

                        // 9. Return Dashboard
                        var returnDashboardRequest = new ReturnDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetReturnDashboardAsync(returnDashboardRequest);
                        await Task.Delay(500, stoppingToken);

                        // 10. DC Validation
                        var dcValidateRequest = new DcValidateDashboardQueryRequest { UserId = superAdminIdStr, PageIndex = 1, PageSize = 100 };
                        var dcValidateData = await liveStockService.GetDcValidateDashboardAsync(dcValidateRequest);
                        await _diffEngine.ProcessDcValidationDiffAsync(dcValidateData, stoppingToken);
                        await Task.Delay(500, stoppingToken);

                        // 11. Tag Cycle Count
                        var tagCycleCountRequest = new TagCycleCountQueryRequest { SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "CYCLE_COUNT", SortDirection = "DESC" };
                        await liveStockService.GetTagCycleCountDataAsync(tagCycleCountRequest);

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

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("CacheWarmerService is stopping.");
        }
    }
}
