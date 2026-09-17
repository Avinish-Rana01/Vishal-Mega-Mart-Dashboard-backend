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

                        var liveStockRequest = new LiveStockQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetLiveStockDetailsAsync(liveStockRequest); await Task.Delay(1000, stoppingToken);

                        var cycleCountRequest = new CycleCountDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "STORE CODE", SortDirection = "ASC" };
                        await liveStockService.GetCycleCountDashboardAsync(cycleCountRequest); await Task.Delay(1000, stoppingToken);

                        var vendorHuRequest = new VendorHUDiscrepancyQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "DIFF_TILL_DATE", SortDirection = "asc" };
                        await liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest); await Task.Delay(1000, stoppingToken);

                        var tagRequest = new TagManagementQueryRequest();
                        await liveStockService.GetTagManagementDataAsync(tagRequest); await Task.Delay(1000, stoppingToken);

                        var encodeRequest = new WarehouseEncodingQueryRequest { FromDate = DateTime.Now.ToString("yyyy-MM-dd"), ToDate = DateTime.Now.ToString("yyyy-MM-dd") };
                        await liveStockService.GetWarehouseEncodingDataAsync(encodeRequest); await Task.Delay(1000, stoppingToken);

                        var storeDashboardRequest = new StoreDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "Store", SortDirection = "asc" };
                        await liveStockService.GetStoreDashboardAsync(storeDashboardRequest); await Task.Delay(1000, stoppingToken);

                        var saleDashboardRequest = new SaleDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetSaleDashboardAsync(saleDashboardRequest); await Task.Delay(1000, stoppingToken);

                        var voidDashboardRequest = new VoidDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetVoidDashboardAsync(voidDashboardRequest); await Task.Delay(1000, stoppingToken);

                        var returnDashboardRequest = new ReturnDashboardQueryRequest { UserId = superAdminIdStr, SearchTerm = "", PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetReturnDashboardAsync(returnDashboardRequest); await Task.Delay(1000, stoppingToken);

                        var dcValidateRequest = new DcValidateDashboardQueryRequest { UserId = superAdminIdStr, PageIndex = 1, PageSize = 100 };
                        await liveStockService.GetDcValidateDashboardAsync(dcValidateRequest); await Task.Delay(1000, stoppingToken);

                        var tagCycleCountRequest = new TagCycleCountQueryRequest { SearchTerm = "", PageIndex = 1, PageSize = 100, SortColumn = "CYCLE_COUNT", SortDirection = "DESC" };
                        await liveStockService.GetTagCycleCountDataAsync(tagCycleCountRequest); await Task.Delay(1000, stoppingToken);

                        TotalRuns++;
                        LastRunTime = DateTime.Now;
                        LastStatus = $"Pre-warmed successfully (Iteration #{TotalRuns})";
                        _logger.LogInformation("Cache successfully pre-warmed for Super Admin. Total runs: {runs}", TotalRuns);

                        try
                        {
                            System.IO.Directory.CreateDirectory("logs");
                            System.IO.File.AppendAllText("logs/cache_warmer.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Cache pre-warmed successfully. Iteration #{TotalRuns}\n");
                        }
                        catch { /* non-blocking file append */ }
                    }
                }
                catch (Exception ex)
                {
                    LastStatus = $"Error: {ex.Message}";
                    _logger.LogError(ex, "Error occurred while pre-warming the cache.");
                }

                await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
            }

            _logger.LogInformation("CacheWarmerService is stopping.");
        }
    }
}
