using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Dashboard.Hubs;

namespace VS_Mart_Backend.Services
{
    /// <summary>
    /// Event-Driven SQL Notification Service powered by SQL Server Service Broker & SqlDependency.
    /// Replaces active polling by receiving instant push callbacks when tbl_Encoding_Dtl or tbl_GRC_DETAILS are modified.
    /// </summary>
    public class SqlNotificationService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<SqlNotificationService> _logger;
        private readonly string _connectionString;

        public static bool IsListening { get; private set; } = false;
        public static long TotalEventsReceived { get; private set; } = 0;
        public static DateTime? LastEventTime { get; private set; }
        public static string LastTriggerSource { get; private set; } = "None";

        public SqlNotificationService(
            IConfiguration configuration,
            IMemoryCache cache,
            IHubContext<DashboardHub> hubContext,
            ILogger<SqlNotificationService> logger)
        {
            _configuration = configuration;
            _cache = cache;
            _hubContext = hubContext;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("POS")
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? _configuration.GetConnectionString("POSConnection")
                ?? string.Empty;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("SqlNotificationService: Connection string 'POS' is empty. Notification service cannot start.");
                return;
            }

            try
            {
                // Start SQL Server Service Broker Dependency Listener
                _logger.LogInformation("SqlNotificationService: Starting SqlDependency listener on database...");
                SqlDependency.Start(_connectionString);
                IsListening = true;
                _logger.LogInformation("SqlNotificationService: SqlDependency started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SqlNotificationService: Failed to start SqlDependency. Check Service Broker permissions.");
                return;
            }

            // Register initial subscriptions for Phase 1 (LiveStock & Store Validation)
            RegisterLiveStockSubscription();
            RegisterStoreValidationSubscription();

            // Keep service alive while running
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            try
            {
                SqlDependency.Stop(_connectionString);
                IsListening = false;
                _logger.LogInformation("SqlNotificationService: SqlDependency stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SqlNotificationService: Error stopping SqlDependency.");
            }
        }

        /// <summary>
        /// Registers a Service Broker notification subscription on tbl_Encoding_Dtl (Live Stock modifications)
        /// </summary>
        private void RegisterLiveStockSubscription()
        {
            Task.Run(async () =>
            {
                try
                {
                    using var conn = new SqlConnection(_connectionString);
                    await conn.OpenAsync();

                    // Query must use explicit two-part table name without SELECT * for SqlDependency
                    using var cmd = new SqlCommand("SELECT Encode_Trans_ID, STORE_ID, Is_Status FROM dbo.tbl_Encoding_Dtl", conn);
                    cmd.Notification = null;

                    var dependency = new SqlDependency(cmd);
                    dependency.OnChange += OnLiveStockChanged;

                    using var reader = await cmd.ExecuteReaderAsync();
                    // Read minimal rows to register subscription
                    int readCount = 0;
                    while (await reader.ReadAsync() && readCount < 5)
                    {
                        readCount++;
                    }
                    _logger.LogInformation("SqlNotificationService: Registered subscription for LiveStock (tbl_Encoding_Dtl).");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SqlNotificationService: Error registering LiveStock subscription. Retrying in 10s...");
                    await Task.Delay(10000);
                    RegisterLiveStockSubscription();
                }
            });
        }

        private void OnLiveStockChanged(object sender, SqlNotificationEventArgs e)
        {
            _logger.LogInformation("SqlNotificationService: OnLiveStockChanged triggered - Type: {Type}, Source: {Source}, Info: {Info}", e.Type, e.Source, e.Info);

            if (e.Type == SqlNotificationType.Change)
            {
                TotalEventsReceived++;
                LastEventTime = DateTime.UtcNow;
                LastTriggerSource = "tbl_Encoding_Dtl (Live Stock)";
                _logger.LogInformation("SqlNotificationService: >>> DB CHANGE EVENT <<< LiveStock modified!");

                // Invalidate LiveStock memory cache so fresh data is computed
                _cache.Remove("LiveStockDetails_Master__STORE_asc_string");

                // Trigger LiveStock poller delta check immediately
                LiveStockPollerService.TriggerImmediatePoll();
            }

            // SqlDependency subscriptions fire once and must be re-registered
            RegisterLiveStockSubscription();
        }

        /// <summary>
        /// Registers a Service Broker notification subscription on tbl_GRC_DETAILS (Store Validation modifications)
        /// </summary>
        private void RegisterStoreValidationSubscription()
        {
            Task.Run(async () =>
            {
                try
                {
                    using var conn = new SqlConnection(_connectionString);
                    await conn.OpenAsync();

                    // Query must use explicit schema and selected columns
                    using var cmd = new SqlCommand("SELECT GRC_ID, STORE_CODE, HU, Is_Status FROM dbo.tbl_GRC_DETAILS", conn);
                    cmd.Notification = null;

                    var dependency = new SqlDependency(cmd);
                    dependency.OnChange += OnStoreValidationChanged;

                    using var reader = await cmd.ExecuteReaderAsync();
                    int readCount = 0;
                    while (await reader.ReadAsync() && readCount < 5)
                    {
                        readCount++;
                    }
                    _logger.LogInformation("SqlNotificationService: Registered subscription for StoreValidation (tbl_GRC_DETAILS).");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SqlNotificationService: Error registering StoreValidation subscription. Retrying in 10s...");
                    await Task.Delay(10000);
                    RegisterStoreValidationSubscription();
                }
            });
        }

        private void OnStoreValidationChanged(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                TotalEventsReceived++;
                LastEventTime = DateTime.UtcNow;
                LastTriggerSource = "tbl_GRC_DETAILS (Store Validation)";
                _logger.LogInformation("SqlNotificationService: >>> DB CHANGE EVENT <<< Store Validation modified! Source: {Source}, Info: {Info}", e.Source, e.Info);

                // Trigger Store Validation poller check immediately
                DashboardSectionsPollerService.TriggerStoreValidationPoll();
            }

            // Re-register subscription
            RegisterStoreValidationSubscription();
        }
    }
}
