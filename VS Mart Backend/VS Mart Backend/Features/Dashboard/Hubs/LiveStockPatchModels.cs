using System;

namespace VS_Mart_Backend.Features.Dashboard.Hubs
{
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
}
