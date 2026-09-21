using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VS_Mart_Backend.Features.Dashboard.Hubs;
using VS_Mart_Backend.Features.MainDashboard;

namespace VS_Mart_Backend.Features.Dashboard.DiffEngine
{
    public class DashboardDiffEngine : IDashboardDiffEngine
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardDiffEngine> _logger;

        // ── 1. LiveStock Snapshots ───────────────────────────────────────────
        private readonly ConcurrentDictionary<string, LiveStockSnapshot> _liveStockSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private int _lastTotalRfid = 0;
        private int _lastTotalDiff = 0;
        private bool _isLiveStockInitialized = false;

        private class LiveStockSnapshot
        {
            public int RfidStock { get; set; }
            public int SapStock { get; set; }
            public int Difference { get; set; }
            public decimal Percentage { get; set; }
            public string? StoreName { get; set; }
        }

        // ── 2. Cycle Count Snapshots ─────────────────────────────────────────
        private readonly ConcurrentDictionary<string, CycleSnapshot> _cycleSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private bool _isCycleCountInitialized = false;

        private class CycleSnapshot
        {
            public int ScannedQty { get; set; }
            public int SystemStock { get; set; }
            public int NetDiff { get; set; }
            public int NoOfArticles { get; set; }
            public int ShortQty { get; set; }
            public int ExcessQty { get; set; }
        }

        // ── 3. Vendor Discrepancy Snapshots ──────────────────────────────────
        private readonly ConcurrentDictionary<string, VendorSnapshot> _vendorSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private bool _isVendorInitialized = false;

        private class VendorSnapshot
        {
            public int ScannedQty { get; set; }
            public int ActualQty { get; set; }
            public int DiffQty { get; set; }
            public int DiffTillDate { get; set; }
        }

        // ── 4. Store Validation Snapshots ────────────────────────────────────
        private readonly ConcurrentDictionary<string, StoreValidationSnapshot> _storeValidationSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private bool _isStoreValidationInitialized = false;

        private class StoreValidationSnapshot
        {
            public int HuReceived { get; set; }
            public int HuValidated { get; set; }
            public int HuWrong { get; set; }
            public int HhtValidate { get; set; }
            public int Encoded { get; set; }
            public int StorePending { get; set; }
        }

        // ── 5. DC Encoding Snapshots ─────────────────────────────────────────
        private int _lastEncodingTotal = 0;
        private Dictionary<string, int> _lastEncodingHours = new(StringComparer.OrdinalIgnoreCase);
        private bool _isEncodingInitialized = false;

        // ── 6. Tag Management Snapshots ──────────────────────────────────────
        private int _lastTagStoreCount = 0;
        private int _lastTagWarehouseCount = 0;
        private bool _isTagManagementInitialized = false;

        // ── 7. DC Validation Snapshots ───────────────────────────────────────
        private readonly ConcurrentDictionary<string, DcValidationSnapshot> _dcValidationSnapshots = new(StringComparer.OrdinalIgnoreCase);
        private bool _isDcValidationInitialized = false;

        private class DcValidationSnapshot
        {
            public int ProcessedHu { get; set; }
            public int UnprocessedHu { get; set; }
            public int ProcessedArticleQty { get; set; }
        }

        public DashboardDiffEngine(IHubContext<DashboardHub> hubContext, ILogger<DashboardDiffEngine> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        // =====================================================================
        // 1. LiveStock Diff
        // =====================================================================
        public async Task ProcessLiveStockDiffAsync(LiveStockResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Items == null) return;

            try
            {
                int currentTotalRfid = response.Summary?.RfidQty ?? 0;
                int currentTotalDiff = response.Summary?.DiffQty ?? 0;

                if (!_isLiveStockInitialized)
                {
                    foreach (var row in response.Items)
                    {
                        string storeCode = GetString(row, "STORE_CODE");
                        if (!string.IsNullOrEmpty(storeCode))
                        {
                            _liveStockSnapshots[storeCode] = new LiveStockSnapshot
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
                    _isLiveStockInitialized = true;
                    _logger.LogInformation("DashboardDiffEngine: LiveStock baseline initialized with {Count} stores.", _liveStockSnapshots.Count);
                    return;
                }

                foreach (var row in response.Items)
                {
                    string storeCode = GetString(row, "STORE_CODE");
                    if (string.IsNullOrEmpty(storeCode)) continue;

                    int newRfid = GetInt(row, "RFID_STOCK");
                    int newSap = GetInt(row, "SAP_STOCK");
                    int newDiff = GetInt(row, "DIFFERENCE");
                    decimal newPct = GetDecimal(row, "PERCENTAGE");
                    string storeName = GetString(row, "STORE_NAME");

                    if (_liveStockSnapshots.TryGetValue(storeCode, out var oldSnapshot))
                    {
                        if (oldSnapshot.RfidStock != newRfid || oldSnapshot.Difference != newDiff)
                        {
                            int deltaRfid = newRfid - oldSnapshot.RfidStock;
                            int deltaDiff = newDiff - oldSnapshot.Difference;

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

                            _logger.LogInformation("DashboardDiffEngine: LiveStock patch for Store {StoreCode}: Delta RFID {DeltaRfid:+0;-#}", storeCode, deltaRfid);
                            await _hubContext.Clients.All.SendAsync("ReceiveLiveStockPatch", patch, cancellationToken);
                        }
                    }
                    else
                    {
                        _liveStockSnapshots[storeCode] = new LiveStockSnapshot
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing LiveStock diff.");
            }
        }

        // =====================================================================
        // 2. Cycle Count Diff
        // =====================================================================
        public async Task ProcessCycleCountDiffAsync(CycleCountDashboardResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Items == null) return;

            try
            {
                int recordCount = response.Summary?.RecordCount ?? 0;
                int totalRefNo = response.Summary?.RefNo ?? 0;

                if (!_isCycleCountInitialized)
                {
                    foreach (var row in response.Items)
                    {
                        string refNo = GetString(row, "REF_NO");
                        string storeCode = GetString(row, "STORE_CODE");
                        string key = !string.IsNullOrEmpty(refNo) ? refNo : storeCode;
                        if (!string.IsNullOrEmpty(key))
                        {
                            _cycleSnapshots[key] = new CycleSnapshot
                            {
                                ScannedQty = GetInt(row, "SCANNED_QTY"),
                                SystemStock = GetInt(row, "SYSTEM_STOCK"),
                                NetDiff = GetInt(row, "NET_DIFF"),
                                NoOfArticles = GetInt(row, "NO_OF_ARTICLE"),
                                ShortQty = GetInt(row, "SHORT_QTY"),
                                ExcessQty = GetInt(row, "EXCESS_QTY")
                            };
                        }
                    }
                    _isCycleCountInitialized = true;
                    _logger.LogInformation("DashboardDiffEngine: CycleCount baseline initialized with {Count} entries.", _cycleSnapshots.Count);
                    return;
                }

                foreach (var row in response.Items)
                {
                    string refNo = GetString(row, "REF_NO");
                    string storeCode = GetString(row, "STORE_CODE");
                    string key = !string.IsNullOrEmpty(refNo) ? refNo : storeCode;
                    if (string.IsNullOrEmpty(key)) continue;

                    int scannedQty = GetInt(row, "SCANNED_QTY");
                    int systemStock = GetInt(row, "SYSTEM_STOCK");
                    int netDiff = GetInt(row, "NET_DIFF");
                    int noOfArticles = GetInt(row, "NO_OF_ARTICLE");
                    int shortQty = GetInt(row, "SHORT_QTY");
                    int excessQty = GetInt(row, "EXCESS_QTY");
                    string storeName = GetString(row, "STORE_NAME");

                    if (_cycleSnapshots.TryGetValue(key, out var oldSnap))
                    {
                        if (oldSnap.ScannedQty != scannedQty || oldSnap.NetDiff != netDiff)
                        {
                            int deltaScanned = scannedQty - oldSnap.ScannedQty;
                            int deltaNetDiff = netDiff - oldSnap.NetDiff;

                            oldSnap.ScannedQty = scannedQty;
                            oldSnap.SystemStock = systemStock;
                            oldSnap.NetDiff = netDiff;
                            oldSnap.NoOfArticles = noOfArticles;
                            oldSnap.ShortQty = shortQty;
                            oldSnap.ExcessQty = excessQty;

                            var patch = new CycleCountDeltaPatch
                            {
                                Type = "CYCLE_COUNT_DELTA",
                                Timestamp = DateTime.UtcNow,
                                RefNo = refNo,
                                StoreCode = storeCode,
                                StoreName = storeName,
                                DeltaScannedQty = deltaScanned,
                                DeltaNetDiff = deltaNetDiff,
                                NewScannedQty = scannedQty,
                                NewSystemStock = systemStock,
                                NewNetDifference = netDiff,
                                NewNoOfArticles = noOfArticles,
                                NewShortQty = shortQty,
                                NewExcessQty = excessQty,
                                SummaryDelta = new CycleCountSummaryDelta
                                {
                                    RecordCount = recordCount,
                                    TotalRefNo = totalRefNo
                                }
                            };

                            _logger.LogInformation("DashboardDiffEngine: CycleCount patch for Key {Key}: Delta Scanned {DeltaScanned:+0;-#}", key, deltaScanned);
                            await _hubContext.Clients.All.SendAsync("ReceiveCycleCountPatch", patch, cancellationToken);
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
                _logger.LogError(ex, "DashboardDiffEngine: Error processing CycleCount diff.");
            }
        }

        // =====================================================================
        // 3. Vendor Discrepancy Diff
        // =====================================================================
        public async Task ProcessVendorDiscrepancyDiffAsync(VendorHUDiscrepancyResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Items == null) return;

            try
            {
                var summary = response.Summary;

                if (!_isVendorInitialized)
                {
                    foreach (var row in response.Items)
                    {
                        string key = GetString(row, "VENDOR_NAME");
                        if (string.IsNullOrEmpty(key)) key = GetString(row, "VENDOR_CODE");
                        if (!string.IsNullOrEmpty(key))
                        {
                            _vendorSnapshots[key] = new VendorSnapshot
                            {
                                ScannedQty = GetInt(row, "SCANNED_QTY"),
                                ActualQty = GetInt(row, "ACTUAL_QTY"),
                                DiffQty = GetInt(row, "DIFF_QTY"),
                                DiffTillDate = GetInt(row, "DIFF_TILL_DATE")
                            };
                        }
                    }
                    _isVendorInitialized = true;
                    return;
                }

                foreach (var row in response.Items)
                {
                    string vendorName = GetString(row, "VENDOR_NAME");
                    string vendorCode = GetString(row, "VENDOR_CODE");
                    string key = !string.IsNullOrEmpty(vendorName) ? vendorName : vendorCode;
                    if (string.IsNullOrEmpty(key)) continue;

                    int scanned = GetInt(row, "SCANNED_QTY");
                    int actual = GetInt(row, "ACTUAL_QTY");
                    int diff = GetInt(row, "DIFF_QTY");
                    int tillDate = GetInt(row, "DIFF_TILL_DATE");

                    if (_vendorSnapshots.TryGetValue(key, out var oldSnap))
                    {
                        if (oldSnap.ScannedQty != scanned || oldSnap.DiffQty != diff)
                        {
                            int deltaScanned = scanned - oldSnap.ScannedQty;
                            int deltaDiff = diff - oldSnap.DiffQty;

                            oldSnap.ScannedQty = scanned;
                            oldSnap.ActualQty = actual;
                            oldSnap.DiffQty = diff;
                            oldSnap.DiffTillDate = tillDate;

                            var patch = new VendorDiscrepancyDeltaPatch
                            {
                                Type = "VENDOR_DISCREPANCY_DELTA",
                                Timestamp = DateTime.UtcNow,
                                VendorName = vendorName,
                                VendorCode = vendorCode,
                                DeltaScannedQty = deltaScanned,
                                DeltaDiffQty = deltaDiff,
                                NewActualQty = actual,
                                NewScannedQty = scanned,
                                NewDifferenceQty = diff,
                                NewDifferenceQtyTillDate = tillDate,
                                SummaryDelta = new VendorDiscrepancySummaryDelta
                                {
                                    TotalActualQty = summary?.ActualQty ?? 0,
                                    TotalScannedQty = summary?.ScannedQty ?? 0,
                                    TotalDifferenceQty = summary?.DifferenceQty ?? 0,
                                    TotalDifferenceTillDate = summary?.DifferenceQtyTillDate ?? 0
                                }
                            };

                            _logger.LogInformation("DashboardDiffEngine: VendorDiscrepancy patch for {Vendor}: Delta Diff {DeltaDiff:+0;-#}", key, deltaDiff);
                            await _hubContext.Clients.All.SendAsync("ReceiveVendorDiscrepancyPatch", patch, cancellationToken);
                        }
                    }
                    else
                    {
                        _vendorSnapshots[key] = new VendorSnapshot
                        {
                            ScannedQty = scanned,
                            ActualQty = actual,
                            DiffQty = diff,
                            DiffTillDate = tillDate
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing VendorDiscrepancy diff.");
            }
        }

        // =====================================================================
        // 4. Store Validation Diff
        // =====================================================================
        public async Task ProcessStoreValidationDiffAsync(StoreDashboardResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Items == null) return;

            try
            {
                var summary = response.Summary;

                if (!_isStoreValidationInitialized)
                {
                    foreach (var row in response.Items)
                    {
                        string storeCode = GetString(row, "Store");
                        if (string.IsNullOrEmpty(storeCode)) storeCode = GetString(row, "STORE_CODE");
                        if (!string.IsNullOrEmpty(storeCode))
                        {
                            _storeValidationSnapshots[storeCode] = new StoreValidationSnapshot
                            {
                                HuReceived = GetInt(row, "HU_RECEIVED_QTY"),
                                HuValidated = GetInt(row, "HU_VALIDATED_QTY"),
                                HuWrong = GetInt(row, "HU_WRONG_QTY"),
                                HhtValidate = GetInt(row, "HHT_VALIDATE_QTY"),
                                Encoded = GetInt(row, "ENCODED_QTY"),
                                StorePending = GetInt(row, "STORE_PENDING_QTY")
                            };
                        }
                    }
                    _isStoreValidationInitialized = true;
                    return;
                }

                foreach (var row in response.Items)
                {
                    string storeCode = GetString(row, "Store");
                    if (string.IsNullOrEmpty(storeCode)) storeCode = GetString(row, "STORE_CODE");
                    if (string.IsNullOrEmpty(storeCode)) continue;

                    string storeName = GetString(row, "StoreName");
                    int huReceived = GetInt(row, "HU_RECEIVED_QTY");
                    int huValidated = GetInt(row, "HU_VALIDATED_QTY");
                    int huWrong = GetInt(row, "HU_WRONG_QTY");
                    int hhtValidate = GetInt(row, "HHT_VALIDATE_QTY");
                    int encoded = GetInt(row, "ENCODED_QTY");
                    int storePending = GetInt(row, "STORE_PENDING_QTY");

                    if (_storeValidationSnapshots.TryGetValue(storeCode, out var oldSnap))
                    {
                        if (oldSnap.HuValidated != huValidated || oldSnap.HuWrong != huWrong || oldSnap.Encoded != encoded)
                        {
                            int deltaValidated = huValidated - oldSnap.HuValidated;
                            int deltaWrong = huWrong - oldSnap.HuWrong;
                            int deltaHht = hhtValidate - oldSnap.HhtValidate;
                            int deltaEncoded = encoded - oldSnap.Encoded;

                            oldSnap.HuReceived = huReceived;
                            oldSnap.HuValidated = huValidated;
                            oldSnap.HuWrong = huWrong;
                            oldSnap.HhtValidate = hhtValidate;
                            oldSnap.Encoded = encoded;
                            oldSnap.StorePending = storePending;

                            var patch = new StoreValidationDeltaPatch
                            {
                                Type = "STORE_VALIDATION_DELTA",
                                Timestamp = DateTime.UtcNow,
                                StoreCode = storeCode,
                                StoreName = storeName,
                                DeltaHuValidated = deltaValidated,
                                DeltaHuWrong = deltaWrong,
                                DeltaHhtValidate = deltaHht,
                                DeltaEncoded = deltaEncoded,
                                NewHuReceivedQty = huReceived,
                                NewHuValidatedQty = huValidated,
                                NewHuWrongQty = huWrong,
                                NewHhtValidateQty = hhtValidate,
                                NewEncodedQty = encoded,
                                NewStorePendingQty = storePending,
                                SummaryDelta = new StoreValidationSummaryDelta
                                {
                                    TotalHuReceived = summary?.HuReceivedQty ?? 0,
                                    TotalHuValidated = summary?.HuValidatedQty ?? 0,
                                    TotalHuWrong = summary?.HuWrongQty ?? 0,
                                    TotalHhtValidate = summary?.HhtValidateQty ?? 0,
                                    TotalEncoded = summary?.EncodedQty ?? 0,
                                    TotalPending = (summary?.HuReceivedQty ?? 0) - (summary?.HuValidatedQty ?? 0)
                                }
                            };

                            _logger.LogInformation("DashboardDiffEngine: StoreValidation patch for {StoreCode}: Delta Validated {DeltaValidated:+0;-#}", storeCode, deltaValidated);
                            await _hubContext.Clients.All.SendAsync("ReceiveStoreValidationPatch", patch, cancellationToken);
                        }
                    }
                    else
                    {
                        _storeValidationSnapshots[storeCode] = new StoreValidationSnapshot
                        {
                            HuReceived = huReceived,
                            HuValidated = huValidated,
                            HuWrong = huWrong,
                            HhtValidate = hhtValidate,
                            Encoded = encoded,
                            StorePending = storePending
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing StoreValidation diff.");
            }
        }

        // =====================================================================
        // 5. DC Encoding Diff
        // =====================================================================
        public async Task ProcessWarehouseEncodingDiffAsync(WarehouseEncodingResponse response, CancellationToken cancellationToken = default)
        {
            if (response == null) return;

            try
            {
                int currentTotal = 0;
                var hourCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (response.Summary != null)
                {
                    hourCounts["08 - 09"] = response.Summary.Hour8To9;
                    hourCounts["09 - 10"] = response.Summary.Hour9To10;
                    hourCounts["10 - 11"] = response.Summary.Hour10To11;
                    hourCounts["11 - 12"] = response.Summary.Hour11To12;
                    hourCounts["12 - 13"] = response.Summary.Hour12To13;
                    hourCounts["13 - 14"] = response.Summary.Hour13To14;
                    hourCounts["14 - 15"] = response.Summary.Hour14To15;
                    hourCounts["15 - 16"] = response.Summary.Hour15To16;
                    hourCounts["16 - 17"] = response.Summary.Hour16To17;
                    hourCounts["17 - 18"] = response.Summary.Hour17To18;
                    hourCounts["18 - 19"] = response.Summary.Hour18To19;
                    hourCounts["19 - 20"] = response.Summary.Hour19To20;

                    currentTotal = response.Summary.Hour8To9 + response.Summary.Hour9To10 +
                                   response.Summary.Hour10To11 + response.Summary.Hour11To12 +
                                   response.Summary.Hour12To13 + response.Summary.Hour13To14 +
                                   response.Summary.Hour14To15 + response.Summary.Hour15To16 +
                                   response.Summary.Hour16To17 + response.Summary.Hour17To18 +
                                   response.Summary.Hour18To19 + response.Summary.Hour19To20;
                }
                else if (response.Items != null)
                {
                    currentTotal = response.Items.Sum(r => GetInt(r, "COUNT"));
                }

                if (!_isEncodingInitialized)
                {
                    _lastEncodingTotal = currentTotal;
                    _lastEncodingHours = new Dictionary<string, int>(hourCounts, StringComparer.OrdinalIgnoreCase);
                    _isEncodingInitialized = true;
                    return;
                }

                bool hoursChanged = false;
                if (hourCounts.Count > 0)
                {
                    foreach (var kvp in hourCounts)
                    {
                        if (!_lastEncodingHours.TryGetValue(kvp.Key, out int lastVal) || lastVal != kvp.Value)
                        {
                            hoursChanged = true;
                            break;
                        }
                    }
                }

                if (currentTotal != _lastEncodingTotal || hoursChanged)
                {
                    int delta = currentTotal - _lastEncodingTotal;
                    _lastEncodingTotal = currentTotal;
                    _lastEncodingHours = new Dictionary<string, int>(hourCounts, StringComparer.OrdinalIgnoreCase);

                    var patch = new DcEncodingDeltaPatch
                    {
                        Type = "DC_ENCODING_DELTA",
                        Timestamp = DateTime.UtcNow,
                        TimeBlock = "TOTAL",
                        DeltaCount = delta,
                        NewCount = currentTotal,
                        TotalCount = currentTotal,
                        AllHourCounts = hourCounts.Count > 0 ? hourCounts : null
                    };

                    _logger.LogInformation("DashboardDiffEngine: DcEncoding patch: Delta {Delta:+0;-#}, Total: {Total}", delta, currentTotal);
                    await _hubContext.Clients.All.SendAsync("ReceiveDcEncodingPatch", patch, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing WarehouseEncoding diff.");
            }
        }

        // =====================================================================
        // 6. Tag Management Diff
        // =====================================================================
        public async Task ProcessTagManagementDiffAsync(TagManagementResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Summary == null) return;

            try
            {
                int storeCount = response.Summary.StoreCount;
                int warehouseCount = response.Summary.WarehouseCount;

                if (!_isTagManagementInitialized)
                {
                    _lastTagStoreCount = storeCount;
                    _lastTagWarehouseCount = warehouseCount;
                    _isTagManagementInitialized = true;
                    return;
                }

                if (storeCount != _lastTagStoreCount || warehouseCount != _lastTagWarehouseCount)
                {
                    int deltaStore = storeCount - _lastTagStoreCount;
                    int deltaWarehouse = warehouseCount - _lastTagWarehouseCount;

                    _lastTagStoreCount = storeCount;
                    _lastTagWarehouseCount = warehouseCount;

                    var patch = new TagManagementDeltaPatch
                    {
                        Type = "TAG_MANAGEMENT_DELTA",
                        Timestamp = DateTime.UtcNow,
                        StoreCount = storeCount,
                        WarehouseCount = warehouseCount,
                        RecordCount = response.Summary.RecordCount,
                        AvgRecycle = 0,
                        DeltaStoreCount = deltaStore,
                        DeltaWarehouseCount = deltaWarehouse
                    };

                    _logger.LogInformation("DashboardDiffEngine: TagManagement patch: Delta Store {DeltaStore:+0;-#}", deltaStore);
                    await _hubContext.Clients.All.SendAsync("ReceiveTagManagementPatch", patch, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing TagManagement diff.");
            }
        }

        // =====================================================================
        // 7. DC Validation Diff
        // =====================================================================
        public async Task ProcessDcValidationDiffAsync(DcValidateDashboardResponse response, CancellationToken cancellationToken = default)
        {
            if (response?.Items == null) return;

            try
            {
                var summary = response.Summary;

                if (!_isDcValidationInitialized)
                {
                    foreach (var row in response.Items)
                    {
                        string plant = GetString(row, "RECIVING_PLANT");
                        if (!string.IsNullOrEmpty(plant))
                        {
                            _dcValidationSnapshots[plant] = new DcValidationSnapshot
                            {
                                ProcessedHu = GetInt(row, "PROCESSED_HU"),
                                UnprocessedHu = GetInt(row, "UNPROCESSED_HU"),
                                ProcessedArticleQty = GetInt(row, "PROCESSED_ARTICLE_QTY")
                            };
                        }
                    }
                    _isDcValidationInitialized = true;
                    return;
                }

                foreach (var row in response.Items)
                {
                    string plant = GetString(row, "RECIVING_PLANT");
                    if (string.IsNullOrEmpty(plant)) continue;

                    string storeName = GetString(row, "STORE_NAME");
                    int procHu = GetInt(row, "PROCESSED_HU");
                    int unprocHu = GetInt(row, "UNPROCESSED_HU");
                    int procArt = GetInt(row, "PROCESSED_ARTICLE_QTY");

                    if (_dcValidationSnapshots.TryGetValue(plant, out var oldSnap))
                    {
                        if (oldSnap.ProcessedHu != procHu || oldSnap.UnprocessedHu != unprocHu)
                        {
                            int deltaProc = procHu - oldSnap.ProcessedHu;
                            int deltaUnproc = unprocHu - oldSnap.UnprocessedHu;
                            int deltaArt = procArt - oldSnap.ProcessedArticleQty;

                            oldSnap.ProcessedHu = procHu;
                            oldSnap.UnprocessedHu = unprocHu;
                            oldSnap.ProcessedArticleQty = procArt;

                            var patch = new DcValidationDeltaPatch
                            {
                                Type = "DC_VALIDATION_DELTA",
                                Timestamp = DateTime.UtcNow,
                                RecivingPlant = plant,
                                StoreName = storeName,
                                DeltaProcessedHu = deltaProc,
                                DeltaUnprocessedHu = deltaUnproc,
                                DeltaProcessedArticleQty = deltaArt,
                                NewProcessedHu = procHu,
                                NewUnprocessedHu = unprocHu,
                                NewProcessedArticleQty = procArt,
                                SummaryDelta = new DcValidationSummaryDelta
                                {
                                    RecordCount = summary?.RecordCount ?? 0,
                                    TotalProcessedHu = summary?.ProcessedHu ?? 0,
                                    TotalUnprocessedHu = summary?.UnprocessedHu ?? 0,
                                    TotalProcessedArticleQty = summary?.ArticleQty ?? 0
                                }
                            };

                            _logger.LogInformation("DashboardDiffEngine: DcValidation patch for {Plant}: Delta Processed HU {DeltaProc:+0;-#}", plant, deltaProc);
                            await _hubContext.Clients.All.SendAsync("ReceiveDcValidationPatch", patch, cancellationToken);
                        }
                    }
                    else
                    {
                        _dcValidationSnapshots[plant] = new DcValidationSnapshot
                        {
                            ProcessedHu = procHu,
                            UnprocessedHu = unprocHu,
                            ProcessedArticleQty = procArt
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardDiffEngine: Error processing DcValidation diff.");
            }
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================
        private static string GetString(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null) return val.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetInt(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (int.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed)) return parsed;
            }
            return 0;
        }

        private static decimal GetDecimal(Dictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (decimal.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)) return parsed;
            }
            return 0m;
        }
    }
}
