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
                        string superAdminId = "26";

                        // Clear old caches for Super Admin
                        liveStockService.InvalidateDashboardCache(superAdminId);

                        // Pre-warm Live Stock Details
                        var liveStockRequest = new LiveStockQueryRequest
                        {
                            UserId = superAdminId,
                            SearchTerm = "",
                            PageIndex = 1,
                            PageSize = 100
                        };
                        await liveStockService.GetLiveStockDetailsAsync(liveStockRequest);

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
                        await liveStockService.GetCycleCountDashboardAsync(cycleCountRequest);

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
                        await liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest);

                        // Pre-warm Tag Management
                        var tagRequest = new TagManagementQueryRequest();
                        await liveStockService.GetTagManagementDataAsync(tagRequest);

                        // Pre-warm Warehouse Encoding (for today)
                        var encodeRequest = new WarehouseEncodingQueryRequest
                        {
                            FromDate = DateTime.Now.ToString("yyyy-MM-dd"),
                            ToDate = DateTime.Now.ToString("yyyy-MM-dd")
                        };
                        await liveStockService.GetWarehouseEncodingDataAsync(encodeRequest);

                        _logger.LogInformation("Cache successfully pre-warmed for Super Admin.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while pre-warming the cache.");
                }

                // Wait for 2 minutes before the next run
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }

            _logger.LogInformation("CacheWarmerService is stopping.");
        }
    }
}
