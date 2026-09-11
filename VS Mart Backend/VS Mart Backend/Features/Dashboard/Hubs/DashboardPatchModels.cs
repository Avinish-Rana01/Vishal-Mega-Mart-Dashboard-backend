    using System;
    using System.Collections.Generic;

    namespace VS_Mart_Backend.Features.Dashboard.Hubs
    {
        // ==========================================
        // 1. Live Stock Real-Time Patch Models
        // ==========================================
        public class LiveStockSummaryDelta
        {
            public int TotalRfidDelta { get; set; }
            public int TotalDiffDelta { get; set; }
            public int NewTotalRfid { get; set; }
            public int NewTotalDiff { get; set; }
        }

        public class LiveStockDeltaPatch
        {
            public string Type { get; set; } = "STOCK_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string StoreCode { get; set; } = string.Empty;
            public string? StoreName { get; set; }
            public int DeltaRfid { get; set; }
            public int DeltaDiff { get; set; }
            public int NewRfidStock { get; set; }
            public int NewSapStock { get; set; }
            public int NewDifference { get; set; }
            public decimal NewPercentage { get; set; }
            public LiveStockSummaryDelta? SummaryDelta { get; set; }
        }

        // ==========================================
        // 2. Cycle Count Real-Time Patch Models
        // ==========================================
        public class CycleCountSummaryDelta
        {
            public int RecordCount { get; set; }
            public int TotalRefNo { get; set; }
        }

        public class CycleCountDeltaPatch
        {
            public string Type { get; set; } = "CYCLE_COUNT_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string RefNo { get; set; } = string.Empty;
            public string StoreCode { get; set; } = string.Empty;
            public string? StoreName { get; set; }
            public int DeltaScannedQty { get; set; }
            public int DeltaNetDiff { get; set; }
            public int NewScannedQty { get; set; }
            public int NewSystemStock { get; set; }
            public int NewNetDifference { get; set; }
            public int NewNoOfArticles { get; set; }
            public int NewShortQty { get; set; }
            public int NewExcessQty { get; set; }
            public CycleCountSummaryDelta? SummaryDelta { get; set; }
        }

        // ==========================================
        // 3. Store Validation Real-Time Patch Models
        // ==========================================
        public class StoreValidationSummaryDelta
        {
            public int TotalHuReceived { get; set; }
            public int TotalHuValidated { get; set; }
            public int TotalHuWrong { get; set; }
            public int TotalHhtValidate { get; set; }
            public int TotalEncoded { get; set; }
            public int TotalPending { get; set; }
        }

        public class StoreValidationDeltaPatch
        {
            public string Type { get; set; } = "STORE_VALIDATION_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string StoreCode { get; set; } = string.Empty;
            public string? StoreName { get; set; }
            public int DeltaHuValidated { get; set; }
            public int DeltaHuWrong { get; set; }
            public int DeltaHhtValidate { get; set; }
            public int DeltaEncoded { get; set; }
            public int NewHuReceivedQty { get; set; }
            public int NewHuValidatedQty { get; set; }
            public int NewHuWrongQty { get; set; }
            public int NewHhtValidateQty { get; set; }
            public int NewEncodedQty { get; set; }
            public int NewStorePendingQty { get; set; }
            public StoreValidationSummaryDelta? SummaryDelta { get; set; }
        }

        // ==========================================
        // 4. DC Encoding (Warehouse Encoding) Patch Models
        // ==========================================
        public class DcEncodingDeltaPatch
        {
            public string Type { get; set; } = "DC_ENCODING_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string TimeBlock { get; set; } = string.Empty; // e.g. "08 - 09", "TOTAL"
            public int DeltaCount { get; set; }
            public int NewCount { get; set; }
            public Dictionary<string, int>? AllHourCounts { get; set; }
            public int TotalCount { get; set; }
        }

        // ==========================================
        // 5. Tag Management Location Patch Models
        // ==========================================
        public class TagManagementDeltaPatch
        {
            public string Type { get; set; } = "TAG_MANAGEMENT_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public int StoreCount { get; set; }
            public int WarehouseCount { get; set; }
            public int RecordCount { get; set; }
            public double AvgRecycle { get; set; }
            public int DeltaStoreCount { get; set; }
            public int DeltaWarehouseCount { get; set; }
        }

        // ==========================================
        // 6. Vendor Discrepancy Patch Models
        // ==========================================
        public class VendorDiscrepancySummaryDelta
        {
            public int TotalActualQty { get; set; }
            public int TotalScannedQty { get; set; }
            public int TotalDifferenceQty { get; set; }
            public int TotalDifferenceTillDate { get; set; }
        }

        public class VendorDiscrepancyDeltaPatch
        {
            public string Type { get; set; } = "VENDOR_DISCREPANCY_DELTA";
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string VendorName { get; set; } = string.Empty;
            public string? VendorCode { get; set; }
            public int DeltaScannedQty { get; set; }
            public int DeltaDiffQty { get; set; }
            public int NewActualQty { get; set; }
            public int NewScannedQty { get; set; }
            public int NewDifferenceQty { get; set; }
            public int NewDifferenceQtyTillDate { get; set; }
            public VendorDiscrepancySummaryDelta? SummaryDelta { get; set; }
        }
    }
