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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Dashboard.Hubs;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Services
{
    public class DashboardSectionsPollerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardSectionsPollerService> _logger;
        private readonly string _connectionString;

        // In-memory snapshots for each section
        private readonly ConcurrentDictionary<string, CycleSnapshot> _cycleSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, StoreValidationSnapshot> _storeSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> _dcSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, VendorSnapshot> _vendorSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private TagManagementSnapshot? _lastTagSnapshot;
        private static readonly SemaphoreSlim _pollerDbGate = new(1, 1);

        // Telemetry counters
        public static long TotalTicksExecuted { get; private set; } = 0;
        public static DateTime? LastPollTime { get; private set; }
        public static object? LastCyclePatch { get; private set; }
        public static object? LastStorePatch { get; private set; }
        public static object? LastDcPatch { get; private set; }
        public static object? LastTagPatch { get; private set; }
        public static object? LastVendorPatch { get; private set; }

        public static object GetTelemetry()
        {
            return new
            {
                isRunning = true,
                totalTicksExecuted = TotalTicksExecuted,
                lastPollTime = LastPollTime?.ToString("o"),
                lastCyclePatch = LastCyclePatch,
                lastStorePatch = LastStorePatch,
                lastDcPatch = LastDcPatch,
                lastTagPatch = LastTagPatch,
                lastVendorPatch = LastVendorPatch
            };
        }

        private class CycleSnapshot
        {
            public int ScannedQty { get; set; }
            public int SystemStock { get; set; }
            public int NetDiff { get; set; }
            public int NoOfArticles { get; set; }
            public int ShortQty { get; set; }
            public int ExcessQty { get; set; }
        }

        private class StoreValidationSnapshot
        {
            public int HuReceivedQty { get; set; }
            public int HuValidatedQty { get; set; }
            public int HuWrongQty { get; set; }
            public int HhtValidateQty { get; set; }
            public int EncodedQty { get; set; }
        }

        private class TagManagementSnapshot
        {
            public int StoreCount { get; set; }
            public int WarehouseCount { get; set; }
            public int RecordCount { get; set; }
        }

        private class VendorSnapshot
        {
            public int ActualQty { get; set; }
            public int ScannedQty { get; set; }
            public int DiffQty { get; set; }
            public int DiffTillDate { get; set; }
        }

        public DashboardSectionsPollerService(
            IConfiguration configuration,
            IMemoryCache cache,
            IHubContext<DashboardHub> hubContext,
            ILogger<DashboardSectionsPollerService> logger)
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
            _logger.LogInformation("DashboardSectionsPollerService: Starting background synchronization engine.");

            // Staggered startup delay to give Web API host and Kestrel time to initialize
            try
            {
                await Task.Delay(3000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            int cycleTick = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        cycleTick++;

                        // Hub occupancy check: If no clients are connected and initial priming is done,
                        // do not poll frequently. Only run a gentle baseline refresh every 60s (30 ticks).
                        bool hasSubscribers = DashboardHub.ConnectedClientsCount > 0;
                        if (!hasSubscribers && cycleTick > 1 && cycleTick % 30 != 0)
                        {
                            continue;
                        }

                        TotalTicksExecuted++;
                        LastPollTime = DateTime.UtcNow;

                        // Acquire gate so poller never consumes more than 1 DB connection at a time
                        await _pollerDbGate.WaitAsync(stoppingToken);
                        try
                        {
                            // 1. Cycle Count: Every 4 seconds (every 2 ticks)
                            if (cycleTick % 2 == 0)
                            {
                                await PollCycleCountAsync(stoppingToken);
                            }

                            // 2. Tag Management: Every 6 seconds (every 3 ticks)
                            if (cycleTick % 3 == 0)
                            {
                                await PollTagManagementAsync(stoppingToken);
                            }

                            // 3. DC Encoding: Every 6 seconds (every 3 ticks)
                            if (cycleTick % 3 == 1)
                            {
                                await PollDcEncodingAsync(stoppingToken);
                            }

                            // 4. Store Validation: Every 12 seconds (every 6 ticks)
                            if (cycleTick % 6 == 0)
                            {
                                await PollStoreValidationAsync(stoppingToken);
                            }

                            // 5. Vendor Discrepancy: Every 14 seconds (every 7 ticks)
                            if (cycleTick % 7 == 0)
                            {
                                await PollVendorDiscrepancyAsync(stoppingToken);
                            }
                        }
                        finally
                        {
                            _pollerDbGate.Release();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DashboardSectionsPollerService: Unexpected error during poll cycle.");
                }
            }
        }

        // =========================================================================
        // 1. Cycle Count Poller & Diff Engine
        // =========================================================================
        private async Task PollCycleCountAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "CYCLE_COUNT_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@USER_ID", 26, DbType.Int32);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 100, DbType.Int32);
                parameters.Add("@SortColumn", "STORE CODE", DbType.String, size: 50);
                parameters.Add("@SortDirection", "ASC", DbType.String, size: 10);
                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("[SP_New_Dashboard]", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                var rawItems = (await connection.QueryAsync<dynamic>(cmd)).ToList();

                var items = rawItems
                    .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                int totalRecordCount = parameters.Get<int?>("@RecordCount") ?? items.Count;
                int totalRefNo = parameters.Get<int?>("@QTY") ?? 0;

                // Also fetch enriched graph data for accurate scanned counts
                var reportParams = new DynamicParameters();
                reportParams.Add("@status", "CYCLE_COUNT_REPORT_VIEW", DbType.String, size: 50);
                reportParams.Add("@SearchTerm", "", DbType.String, size: 200);
                reportParams.Add("@PageIndex", 1, DbType.Int32);
                reportParams.Add("@PageSize", 100, DbType.Int32);
                reportParams.Add("@Store_code", "", DbType.String, size: 50);
                reportParams.Add("@fromdate", "", DbType.String, size: 20);
                reportParams.Add("@todate", "", DbType.String, size: 20);
                reportParams.Add("@ref_No", "", DbType.String, size: 50);
                reportParams.Add("@SortColumn", "DATE", DbType.String, size: 50);
                reportParams.Add("@SortDirection", "desc", DbType.String, size: 10);
                reportParams.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                reportParams.Add("@Qty", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var reportCmd = new CommandDefinition("[SP_NEW_REPORT]", reportParams, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                using var multi = await connection.QueryMultipleAsync(reportCmd);
                var graphItems = (await multi.ReadAsync<dynamic>()).ToList();
                var graphRows = graphItems.Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase)).ToList();

                // Merge and Diff
                foreach (var mainRow in items)
                {
                    string refNo = GetString(mainRow, "REF_NO");
                    string storeCode = GetString(mainRow, "STORE_CODE");
                    string storeName = GetString(mainRow, "STORE_NAME");
                    string key = !string.IsNullOrEmpty(refNo) ? refNo : storeCode;
                    if (string.IsNullOrEmpty(key)) continue;

                    var match = graphRows.FirstOrDefault(g =>
                        (!string.IsNullOrEmpty(refNo) && string.Equals(refNo, GetString(g, "Ref_ID"), StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(storeCode) && string.Equals(storeCode, GetString(g, "STORE_CODE"), StringComparison.OrdinalIgnoreCase))
                    );

                    int scannedQty = match != null ? GetInt(match, "SCANNED_QTY") : 0;
                    int systemStock = match != null ? GetInt(match, "SYSTEM_STOCK") : 0;
                    int netDiff = match != null ? GetInt(match, "NET_DIFF") : 0;
                    int noOfArticles = match != null ? GetInt(match, "NO_OF_ARTICLE") : 0;
                    int shortQty = match != null ? GetInt(match, "SHORT_QTY") : 0;
                    int excessQty = match != null ? GetInt(match, "EXCESS_QTY") : 0;

                    if (_cycleSnapshots.TryGetValue(key, out var oldSnap))
                    {
                        if (oldSnap.ScannedQty != scannedQty || oldSnap.NetDiff != netDiff)
                        {
                            var patch = new CycleCountDeltaPatch
                            {
                                Type = "CYCLE_COUNT_DELTA",
                                Timestamp = DateTime.UtcNow,
                                RefNo = refNo,
                                StoreCode = storeCode,
                                StoreName = storeName,
                                DeltaScannedQty = scannedQty - oldSnap.ScannedQty,
                                DeltaNetDiff = netDiff - oldSnap.NetDiff,
                                NewScannedQty = scannedQty,
                                NewSystemStock = systemStock,
                                NewNetDifference = netDiff,
                                NewNoOfArticles = noOfArticles,
                                NewShortQty = shortQty,
                                NewExcessQty = excessQty,
                                SummaryDelta = new CycleCountSummaryDelta
                                {
                                    RecordCount = totalRecordCount,
                                    TotalRefNo = totalRefNo
                                }
                            };

                            LastCyclePatch = patch;
                            oldSnap.ScannedQty = scannedQty;
                            oldSnap.SystemStock = systemStock;
                            oldSnap.NetDiff = netDiff;
                            oldSnap.NoOfArticles = noOfArticles;
                            oldSnap.ShortQty = shortQty;
                            oldSnap.ExcessQty = excessQty;

                            LogDelta("CYCLE COUNT", $"Key: {key} | Scanned: {scannedQty} (Δ {patch.DeltaScannedQty:+0;-#}) | NetDiff: {netDiff}");
                            await _hubContext.Clients.All.SendAsync("ReceiveCycleCountPatch", patch, stoppingToken);
                        }
                    }
                    else
                    {
                        _cycleSnapshots[key] = new CycleSnapshot
                        {
                            ScannedQty = scannedQty,
                            SystemStock = systemStock,
                            NetDiff = netDiff,
                            NoOfArticles = noOfArticles,
                            ShortQty = shortQty,
                            ExcessQty = excessQty
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DashboardSectionsPollerService: PollCycleCountAsync warning: {Msg}", ex.Message);
            }
        }

        // =========================================================================
        // 2. Store Validation Poller & Diff Engine
        // =========================================================================
        private async Task PollStoreValidationAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "STORE_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 100, DbType.Int32);
                parameters.Add("@User_ID", 26, DbType.Int32);
                parameters.Add("@SortColumn", "Store", DbType.String, size: 50);
                parameters.Add("@SortDirection", "asc", DbType.String, size: 10);
                parameters.Add("@SortType", "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_VALIDATED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_WRONG_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HHT_VALIDATE_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ENCODED_QTY", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                var rawItems = (await connection.QueryAsync<dynamic>(cmd)).ToList();

                var items = rawItems
                    .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                int totalReceived = parameters.Get<int?>("@QTY") ?? 0;
                int totalValidated = parameters.Get<int?>("@HU_VALIDATED_QTY") ?? 0;
                int totalWrong = parameters.Get<int?>("@HU_WRONG_QTY") ?? 0;
                int totalHht = parameters.Get<int?>("@HHT_VALIDATE_QTY") ?? 0;
                int totalEncoded = parameters.Get<int?>("@ENCODED_QTY") ?? 0;

                foreach (var row in items)
                {
                    string storeCode = GetString(row, "STORE");
                    if (string.IsNullOrEmpty(storeCode)) storeCode = GetString(row, "STORE_CODE");
                    string storeName = GetString(row, "STORE_NAME");
                    if (string.IsNullOrEmpty(storeCode)) continue;

                    int huReceived = GetInt(row, "HU_RECEIVED_QTY") == 0 ? GetInt(row, "QTY") : GetInt(row, "HU_RECEIVED_QTY");
                    int huValidated = GetInt(row, "HU_VALIDATED_QTY");
                    int huWrong = GetInt(row, "HU_WRONG_QTY");
                    int hhtVal = GetInt(row, "HHT_VALIDATE_QTY");
                    int encoded = GetInt(row, "ENCODED_QTY");
                    int pending = Math.Max(0, huReceived - huValidated);

                    if (_storeSnapshots.TryGetValue(storeCode, out var oldSnap))
                    {
                        if (oldSnap.HuValidatedQty != huValidated || oldSnap.HuWrongQty != huWrong || oldSnap.HhtValidateQty != hhtVal)
                        {
                            var patch = new StoreValidationDeltaPatch
                            {
                                Type = "STORE_VALIDATION_DELTA",
                                Timestamp = DateTime.UtcNow,
                                StoreCode = storeCode,
                                StoreName = storeName,
                                DeltaHuValidated = huValidated - oldSnap.HuValidatedQty,
                                DeltaHuWrong = huWrong - oldSnap.HuWrongQty,
                                DeltaHhtValidate = hhtVal - oldSnap.HhtValidateQty,
                                DeltaEncoded = encoded - oldSnap.EncodedQty,
                                NewHuReceivedQty = huReceived,
                                NewHuValidatedQty = huValidated,
                                NewHuWrongQty = huWrong,
                                NewHhtValidateQty = hhtVal,
                                NewEncodedQty = encoded,
                                NewStorePendingQty = pending,
                                SummaryDelta = new StoreValidationSummaryDelta
                                {
                                    TotalHuReceived = totalReceived,
                                    TotalHuValidated = totalValidated,
                                    TotalHuWrong = totalWrong,
                                    TotalHhtValidate = totalHht,
                                    TotalEncoded = totalEncoded,
                                    TotalPending = Math.Max(0, totalReceived - totalValidated)
                                }
                            };

                            LastStorePatch = patch;
                            oldSnap.HuReceivedQty = huReceived;
                            oldSnap.HuValidatedQty = huValidated;
                            oldSnap.HuWrongQty = huWrong;
                            oldSnap.HhtValidateQty = hhtVal;
                            oldSnap.EncodedQty = encoded;

                            LogDelta("STORE VALIDATION", $"Store: {storeCode} | Validated: {huValidated} (Δ {patch.DeltaHuValidated:+0;-#})");
                            await _hubContext.Clients.All.SendAsync("ReceiveStoreValidationPatch", patch, stoppingToken);
                        }
                    }
                    else
                    {
                        _storeSnapshots[storeCode] = new StoreValidationSnapshot
                        {
                            HuReceivedQty = huReceived,
                            HuValidatedQty = huValidated,
                            HuWrongQty = huWrong,
                            HhtValidateQty = hhtVal,
                            EncodedQty = encoded
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DashboardSectionsPollerService: PollStoreValidationAsync warning: {Msg}", ex.Message);
            }
        }

        // =========================================================================
        // 3. DC Encoding (Warehouse Encoding) Poller
        // =========================================================================
        private async Task PollDcEncodingAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

                parameters.Add("@status", "SHOW_WAREHOUSE_ENCODE_DATA", DbType.String, size: 50);
                parameters.Add("@fromdate", today, DbType.String, size: 20);
                parameters.Add("@todate", today, DbType.String, size: 20);
                parameters.Add("@User_ID", 0, DbType.Int32);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@SortColumn", "", DbType.String, size: 50);
                parameters.Add("@SortDirection", "", DbType.String, size: 10);

                parameters.Add("@8TO9", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@9TO10", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@10TO11", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@11TO12", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@12TO13", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@13TO14", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@14TO15", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@15TO16", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@16TO17", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@17TO18", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@18TO19", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@19TO20", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                await connection.ExecuteAsync(cmd);

                var hourCounts = new Dictionary<string, int>
                {
                    ["08 - 09"] = parameters.Get<int?>("@8TO9") ?? 0,
                    ["09 - 10"] = parameters.Get<int?>("@9TO10") ?? 0,
                    ["10 - 11"] = parameters.Get<int?>("@10TO11") ?? 0,
                    ["11 - 12"] = parameters.Get<int?>("@11TO12") ?? 0,
                    ["12 - 13"] = parameters.Get<int?>("@12TO13") ?? 0,
                    ["13 - 14"] = parameters.Get<int?>("@13TO14") ?? 0,
                    ["14 - 15"] = parameters.Get<int?>("@14TO15") ?? 0,
                    ["15 - 16"] = parameters.Get<int?>("@15TO16") ?? 0,
                    ["16 - 17"] = parameters.Get<int?>("@16TO17") ?? 0,
                    ["17 - 18"] = parameters.Get<int?>("@17TO18") ?? 0,
                    ["18 - 19"] = parameters.Get<int?>("@18TO19") ?? 0,
                    ["19 - 20"] = parameters.Get<int?>("@19TO20") ?? 0
                };

                int totalCount = hourCounts.Values.Sum();
                bool hasChanged = false;
                string changedBlock = "";
                int deltaVal = 0;

                foreach (var kvp in hourCounts)
                {
                    if (_dcSnapshots.TryGetValue(kvp.Key, out int oldVal))
                    {
                        if (oldVal != kvp.Value)
                        {
                            hasChanged = true;
                            changedBlock = kvp.Key;
                            deltaVal = kvp.Value - oldVal;
                            _dcSnapshots[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        _dcSnapshots[kvp.Key] = kvp.Value;
                    }
                }

                if (hasChanged)
                {
                    var patch = new DcEncodingDeltaPatch
                    {
                        Type = "DC_ENCODING_DELTA",
                        Timestamp = DateTime.UtcNow,
                        TimeBlock = changedBlock,
                        DeltaCount = deltaVal,
                        NewCount = hourCounts.GetValueOrDefault(changedBlock, 0),
                        AllHourCounts = hourCounts,
                        TotalCount = totalCount
                    };

                    LastDcPatch = patch;
                    LogDelta("DC ENCODING", $"TimeBlock: {changedBlock} | Delta: {deltaVal:+0;-#} | Total: {totalCount}");
                    await _hubContext.Clients.All.SendAsync("ReceiveDcEncodingPatch", patch, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DashboardSectionsPollerService: PollDcEncodingAsync warning: {Msg}", ex.Message);
            }
        }

        // =========================================================================
        // 4. Tag Management Poller
        // =========================================================================
        private async Task PollTagManagementAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@status", "TAG_MANAGEMENT_LOCATION", DbType.String, size: 50);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 100, DbType.Int32);
                parameters.Add("@SortColumn", "", DbType.String, size: 50);
                parameters.Add("@SortDirection", "asc", DbType.String, size: 10);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@STORECOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@WHCOUNT", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("SP_NEW_REPORT", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                await connection.ExecuteAsync(cmd);

                int recordCount = parameters.Get<int?>("@RecordCount") ?? 0;
                int storeCount = parameters.Get<int?>("@STORECOUNT") ?? 0;
                int whCount = parameters.Get<int?>("@WHCOUNT") ?? 0;

                if (_lastTagSnapshot != null)
                {
                    if (_lastTagSnapshot.StoreCount != storeCount || _lastTagSnapshot.WarehouseCount != whCount)
                    {
                        var patch = new TagManagementDeltaPatch
                        {
                            Type = "TAG_MANAGEMENT_DELTA",
                            Timestamp = DateTime.UtcNow,
                            StoreCount = storeCount,
                            WarehouseCount = whCount,
                            RecordCount = recordCount,
                            DeltaStoreCount = storeCount - _lastTagSnapshot.StoreCount,
                            DeltaWarehouseCount = whCount - _lastTagSnapshot.WarehouseCount
                        };

                        LastTagPatch = patch;
                        _lastTagSnapshot.StoreCount = storeCount;
                        _lastTagSnapshot.WarehouseCount = whCount;
                        _lastTagSnapshot.RecordCount = recordCount;

                        LogDelta("TAG MANAGEMENT", $"Store: {storeCount} (Δ {patch.DeltaStoreCount:+0;-#}) | Warehouse: {whCount} (Δ {patch.DeltaWarehouseCount:+0;-#})");
                        await _hubContext.Clients.All.SendAsync("ReceiveTagManagementPatch", patch, stoppingToken);
                    }
                }
                else
                {
                    _lastTagSnapshot = new TagManagementSnapshot
                    {
                        StoreCount = storeCount,
                        WarehouseCount = whCount,
                        RecordCount = recordCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DashboardSectionsPollerService: PollTagManagementAsync warning: {Msg}", ex.Message);
            }
        }

        // =========================================================================
        // 5. Vendor Discrepancy Poller
        // =========================================================================
        private async Task PollVendorDiscrepancyAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", "HU_DISCREPANCY_VENDOR_DASHBOARD", DbType.String, size: 50);
                parameters.Add("@SearchTerm", "", DbType.String, size: 200);
                parameters.Add("@PageIndex", 1, DbType.Int32);
                parameters.Add("@PageSize", 100, DbType.Int32);
                parameters.Add("@USER_ID", 26, DbType.Int32);
                parameters.Add("@SortColumn", "DIFF_TILL_DATE", DbType.String, size: 50);
                parameters.Add("@SortDirection", "asc", DbType.String, size: 10);
                parameters.Add("@SortType", "string", DbType.String, size: 50);

                parameters.Add("@RecordCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_DIS_ACTUALQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_DIS_SCANNEDQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_DIS_DIFFQTY", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@HU_DIFF_TILL_DATE", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var cmd = new CommandDefinition("SP_New_Dashboard", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cts.Token);
                var rawItems = (await connection.QueryAsync<dynamic>(cmd)).ToList();

                var items = rawItems
                    .Select(x => ((IDictionary<string, object>)x).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                int totalActual = parameters.Get<int?>("@HU_DIS_ACTUALQTY") ?? 0;
                int totalScanned = parameters.Get<int?>("@HU_DIS_SCANNEDQTY") ?? 0;
                int totalDiff = parameters.Get<int?>("@HU_DIS_DIFFQTY") ?? 0;
                int totalTillDate = parameters.Get<int?>("@HU_DIFF_TILL_DATE") ?? 0;

                foreach (var row in items)
                {
                    string vendorName = GetString(row, "VENDOR_NAME");
                    string vendorCode = GetString(row, "VENDOR_CODE");
                    string key = !string.IsNullOrEmpty(vendorName) ? vendorName : vendorCode;
                    if (string.IsNullOrEmpty(key)) continue;

                    int actual = GetInt(row, "ACTUAL_QTY");
                    int scanned = GetInt(row, "SCANNED_QTY");
                    int diff = GetInt(row, "DIFF_QTY");
                    int tillDate = GetInt(row, "DIFF_TILL_DATE");

                    if (_vendorSnapshots.TryGetValue(key, out var oldSnap))
                    {
                        if (oldSnap.ScannedQty != scanned || oldSnap.DiffQty != diff)
                        {
                            var patch = new VendorDiscrepancyDeltaPatch
                            {
                                Type = "VENDOR_DISCREPANCY_DELTA",
                                Timestamp = DateTime.UtcNow,
                                VendorName = vendorName,
                                VendorCode = vendorCode,
                                DeltaScannedQty = scanned - oldSnap.ScannedQty,
                                DeltaDiffQty = diff - oldSnap.DiffQty,
                                NewActualQty = actual,
                                NewScannedQty = scanned,
                                NewDifferenceQty = diff,
                                NewDifferenceQtyTillDate = tillDate,
                                SummaryDelta = new VendorDiscrepancySummaryDelta
                                {
                                    TotalActualQty = totalActual,
                                    TotalScannedQty = totalScanned,
                                    TotalDifferenceQty = totalDiff,
                                    TotalDifferenceTillDate = totalTillDate
                                }
                            };

                            LastVendorPatch = patch;
                            oldSnap.ActualQty = actual;
                            oldSnap.ScannedQty = scanned;
                            oldSnap.DiffQty = diff;
                            oldSnap.DiffTillDate = tillDate;

                            LogDelta("VENDOR DISCREPANCY", $"Vendor: {key} | Scanned: {scanned} (Δ {patch.DeltaScannedQty:+0;-#}) | Diff: {diff}");
                            await _hubContext.Clients.All.SendAsync("ReceiveVendorDiscrepancyPatch", patch, stoppingToken);
                        }
                    }
                    else
                    {
                        _vendorSnapshots[key] = new VendorSnapshot
                        {
                            ActualQty = actual,
                            ScannedQty = scanned,
                            DiffQty = diff,
                            DiffTillDate = tillDate
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DashboardSectionsPollerService: PollVendorDiscrepancyAsync warning: {Msg}", ex.Message);
            }
        }

        private static string GetString(IDictionary<string, object?> row, string key)
        {
            if (row.TryGetValue(key, out var val) && val != null)
                return val.ToString()?.Trim() ?? string.Empty;
            return string.Empty;
        }

        private static int GetInt(IDictionary<string, object?> row, string key)
        {
            if (row.TryGetValue(key, out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), out int parsed)) return parsed;
                if (decimal.TryParse(val.ToString(), out decimal decParsed)) return (int)Math.Round(decParsed);
            }
            return 0;
        }

        private static void LogDelta(string section, string message)
        {
            try
            {
                string logDir = @"C:\publish\logs";
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "dashboard_sections_deltas.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {section} | {message}\r\n");
            }
            catch { }
        }
    }
}
