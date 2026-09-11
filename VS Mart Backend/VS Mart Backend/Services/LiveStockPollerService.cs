using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Dashboard.Hubs;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Services
{
    public class LiveStockPollerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<LiveStockPollerService> _logger;
        private readonly string _connectionString;

        // Snapshot of last known counts per StoreCode in memory
        private readonly ConcurrentDictionary<string, StoreSnapshot> _snapshot = new(StringComparer.OrdinalIgnoreCase);

        // Record of last known summary totals
        private int _lastTotalRfid = 0;
        private int _lastTotalDiff = 0;
        private bool _isInitialized = false;

        // Telemetry properties to inspect poller status live
        public static long TotalTicksExecuted { get; private set; } = 0;
        public static DateTime? LastPollTime { get; private set; }
        public static long LastPollDurationMs { get; private set; }
        public static LiveStockDeltaPatch? LastDetectedDelta { get; private set; }
        public static string LastStatusMessage { get; private set; } = "Starting...";
        private static readonly ConcurrentDictionary<string, object> _currentStoreMetrics = new();

        public static object GetPollerTelemetry()
        {
            return new
            {
                isRunning = true,
                totalTicksExecuted = TotalTicksExecuted,
                lastPollTime = LastPollTime?.ToString("o"),
                lastPollDurationMs = LastPollDurationMs,
                lastStatusMessage = LastStatusMessage,
                lastDetectedDelta = LastDetectedDelta,
                currentSnapshots = _currentStoreMetrics
            };
        }

        private class StoreSnapshot
        {
            public int RfidStock { get; set; }
            public int SapStock { get; set; }
            public int Difference { get; set; }
            public decimal Percentage { get; set; }
            public string? StoreName { get; set; }
        }

        public LiveStockPollerService(
            IConfiguration configuration,
            IMemoryCache cache,
            IHubContext<DashboardHub> hubContext,
            ILogger<LiveStockPollerService> logger)
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
            _logger.LogInformation("LiveStockPollerService: Service starting. Waiting 5s for Kestrel & Auth initialization...");
            
            // Staggered startup: 5-second initial delay so API port binding and Auth are completely ready
            try
            {
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _logger.LogInformation("LiveStockPollerService: Starting 2-second background polling cycle.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        await PollLiveStockAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LiveStockPollerService: Unexpected error during poll cycle.");
                }
            }

            _logger.LogInformation("LiveStockPollerService: Background service stopped.");
        }

        private async Task PollLiveStockAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            // Strict 1.5s cancellation timeout on SQL query to prevent thread pool starvation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(1500));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "LIVE_STOCK_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 100, DbType.Int32);
                parameters.Add("@User_ID", 26, DbType.Int32);
                parameters.Add("@SortColumn", "STORE", DbType.String, size: 50);
                parameters.Add("@SortDirection", "asc", DbType.String, size: 10);
                parameters.Add("@SortType", "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@DIFF_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                var rawItems = (await connection.QueryAsync<dynamic>(cmd)).ToList();
                sw.Stop();

                TotalTicksExecuted++;
                LastPollTime = DateTime.UtcNow;
                LastPollDurationMs = sw.ElapsedMilliseconds;
                LastStatusMessage = $"Running smoothly. Tick #{TotalTicksExecuted} in {LastPollDurationMs}ms";

                var items = rawItems
                    .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                int currentTotalSap = parameters.Get<int?>("@QTY") ?? 0;
                int currentTotalRfid = parameters.Get<int?>("@ENCODED_QTY") ?? 0;
                int currentTotalDiff = parameters.Get<int?>("@DIFF_QTY") ?? 0;
                int currentRecordCount = parameters.Get<int?>("@RecordCount") ?? items.Count;

                // 1. Keep the memory cache pre-warmed so GET /api/stock/live-details returns in < 15ms
                var responseObj = new LiveStockResponse
                {
                    Items = items,
                    Summary = new LiveStockSummary
                    {
                        PageIndex = 1,
                        RecordCount = currentRecordCount,
                        TotalCount = parameters.Get<int?>("@TotalCount") ?? 0,
                        SapQty = currentTotalSap,
                        RfidQty = currentTotalRfid,
                        DiffQty = currentTotalDiff,
                        StoreName = items.Count > 0 && items[0].ContainsKey("STORE_NAME") ? items[0]["STORE_NAME"]?.ToString() : null,
                        Date = null
                    }
                };

                string defaultCacheKey = "LiveStockDetails_26__1_100_STORE_asc_string";
                _cache.Set(defaultCacheKey, responseObj, TimeSpan.FromSeconds(30));

                // 2. Initial baseline population
                if (!_isInitialized)
                {
                    foreach (var row in items)
                    {
                        string storeCode = GetString(row, "STORE_CODE");
                        if (!string.IsNullOrEmpty(storeCode))
                        {
                            _snapshot[storeCode] = new StoreSnapshot
                            {
                                RfidStock = GetInt(row, "RFID_STOCK"),
                                SapStock = GetInt(row, "SAP_STOCK"),
                                Difference = GetInt(row, "DIFFERENCE"),
                                Percentage = GetDecimal(row, "PERCENTAGE"),
                                StoreName = GetString(row, "STORE_NAME")
                            };
                        }
                    }
                    _lastTotalRfid = currentTotalRfid;
                    _lastTotalDiff = currentTotalDiff;
                    _isInitialized = true;
                    return;
                }

                // 3. Diff Engine: Compare fresh rows with snapshot
                foreach (var row in items)
                {
                    string storeCode = GetString(row, "STORE_CODE");
                    if (string.IsNullOrEmpty(storeCode)) continue;

                    int newRfid = GetInt(row, "RFID_STOCK");
                    int newSap = GetInt(row, "SAP_STOCK");
                    int newDiff = GetInt(row, "DIFFERENCE");
                    decimal newPct = GetDecimal(row, "PERCENTAGE");
                    string storeName = GetString(row, "STORE_NAME");

                    if (_snapshot.TryGetValue(storeCode, out var oldSnapshot))
                    {
                        // Check if RFID count or difference changed
                        if (oldSnapshot.RfidStock != newRfid || oldSnapshot.Difference != newDiff)
                        {
                            int deltaRfid = newRfid - oldSnapshot.RfidStock;
                            int deltaDiff = newDiff - oldSnapshot.Difference;

                            // Update snapshot
                            oldSnapshot.RfidStock = newRfid;
                            oldSnapshot.SapStock = newSap;
                            oldSnapshot.Difference = newDiff;
                            oldSnapshot.Percentage = newPct;
                            oldSnapshot.StoreName = storeName;

                            var patch = new LiveStockDeltaPatch
                            {
                                Type = "STOCK_DELTA",
                                Timestamp = DateTime.UtcNow,
                                StoreCode = storeCode,
                                StoreName = storeName,
                                DeltaRfid = deltaRfid,
                                DeltaDiff = deltaDiff,
                                NewRfidStock = newRfid,
                                NewSapStock = newSap,
                                NewDifference = newDiff,
                                NewPercentage = newPct,
                                SummaryDelta = new LiveStockSummaryDelta
                                {
                                    TotalRfidDelta = currentTotalRfid - _lastTotalRfid,
                                    TotalDiffDelta = currentTotalDiff - _lastTotalDiff,
                                    NewTotalRfid = currentTotalRfid,
                                    NewTotalDiff = currentTotalDiff
                                }
                            };

                            LastDetectedDelta = patch;
                            _logger.LogInformation("LiveStockPollerService: Detected delta for Store {StoreCode}: RFID {DeltaRfid:+0;-#}", storeCode, deltaRfid);

                            try
                            {
                                string logDir = @"C:\publish\logs";
                                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                                File.AppendAllText(Path.Combine(logDir, "livestock_deltas.txt"), 
                                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | DB CHANGE DETECTED | Store: {storeCode} | Delta RFID: {deltaRfid:+0;-#} | New Total: {currentTotalRfid}\r\n");
                            }
                            catch { }

                            // Broadcast patch over SignalR to all connected users
                            await _hubContext.Clients.All.SendAsync("ReceiveLiveStockPatch", patch, stoppingToken);
                        }

                        _currentStoreMetrics[storeCode] = new
                        {
                            rfid = newRfid,
                            sap = newSap,
                            diff = newDiff,
                            pct = newPct,
                            lastChecked = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        // New store added dynamically
                        _snapshot[storeCode] = new StoreSnapshot
                        {
                            RfidStock = newRfid,
                            SapStock = newSap,
                            Difference = newDiff,
                            Percentage = newPct,
                            StoreName = storeName
                        };
                    }
                }

                _lastTotalRfid = currentTotalRfid;
                _lastTotalDiff = currentTotalDiff;
            }
            catch (OperationCanceledException)
            {
                // Timeout exceeded (1.5s) - silently skip tick, never freeze thread pool
                _logger.LogWarning("LiveStockPollerService: SQL poll tick timed out (>1.5s). Skipping tick safely.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LiveStockPollerService: Error executing PollLiveStockAsync.");
            }
        }

        private static string GetString(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null) return val.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetInt(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out int parsed)) return parsed;
            }
            return 0;
        }

        private static decimal GetDecimal(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (decimal.TryParse(val.ToString(), out decimal parsed)) return parsed;
            }
            return 0m;
        }
    }
}
