using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Models;

namespace VS_Mart_Backend.Services
{
    public class CacheWarmerService : BackgroundService
    {
        private readonly ILogger<CacheWarmerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CacheWarmerService(ILogger<CacheWarmerService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheWarmerService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("CacheWarmerService running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var liveStockService = scope.ServiceProvider.GetRequiredService<ILiveStockService>();

                        if (!liveStockService.IsCacheEnabled())
                        {
                            _logger.LogInformation("Cache is currently DISABLED. Skipping pre-warming iteration.");
                            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                            continue;
                        }

                        string superAdminId = "26";

                        // Clear old caches for Super Admin
                        // Pre-warm Live Stock Details
                        var liveStockRequest = new LiveStockQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetLiveStockDetailsAsync(liveStockRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Cycle Count Dashboard
                        var cycleCountRequest = new CycleCountDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100,
                            SortColumn = "STORE CODE",
                            SortDirection = "ASC"
                        };
                        await liveStockService.GetCycleCountDashboardAsync(cycleCountRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Vendor HU Discrepancy
                        var vendorHuRequest = new VendorHUDiscrepancyQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100,
                            SortColumn = "DIFF_TILL_DATE",
                            SortDirection = "asc"
                        };
                        await liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Tag Management
                        var tagRequest = new TagManagementQueryRequest();
                        await liveStockService.GetTagManagementDataAsync(tagRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Warehouse Encoding (for today)
                        var encodeRequest = new WarehouseEncodingQueryRequest
                        {
                            FromDate = DateTime.Now.ToString("yyyy-MM-dd"),
                            ToDate = DateTime.Now.ToString("yyyy-MM-dd")
                        };
                        await liveStockService.GetWarehouseEncodingDataAsync(encodeRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Store Dashboard
                        var storeDashboardRequest = new StoreDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100,
                            SortColumn = "Store",
                            SortDirection = "asc"
                        };
                        await liveStockService.GetStoreDashboardAsync(storeDashboardRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Sale Dashboard
                        var saleDashboardRequest = new SaleDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetSaleDashboardAsync(saleDashboardRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Void Dashboard
                        var voidDashboardRequest = new VoidDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetVoidDashboardAsync(voidDashboardRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Return Dashboard
                        var returnDashboardRequest = new ReturnDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetReturnDashboardAsync(returnDashboardRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm DC Validate Dashboard
                        var dcValidateRequest = new DcValidateDashboardQueryRequest
                        {
                            UserId = superAdminId,
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetDcValidateDashboardAsync(dcValidateRequest); await Task.Delay(1000, stoppingToken);

                        // Pre-warm Tag Cycle Count
                        var tagCycleCountRequest = new TagCycleCountQueryRequest
                        {
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100,
                            SortColumn = "CYCLE_COUNT",
                            SortDirection = "DESC"
                        };
                        await liveStockService.GetTagCycleCountDataAsync(tagCycleCountRequest); await Task.Delay(1000, stoppingToken);

                        _logger.LogInformation("Cache successfully pre-warmed for Super Admin.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while pre-warming the cache.");
                }

                // Wait for 2 minutes before the next run
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("CacheWarmerService is stopping.");
        }
    }
}

