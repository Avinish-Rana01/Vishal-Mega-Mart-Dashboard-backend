using System;
using System.Collections.Generic;

namespace VS_Mart_Backend.Features.MainDashboard
{
    public class LiveStockQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "STORE";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class LiveStockSummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int? TotalCount { get; set; }
        public int SapQty { get; set; }
        public int RfidQty { get; set; }
        public int DiffQty { get; set; }
        public string? StoreName { get; set; }
        public string? Date { get; set; }
    }

    public class LiveStockResponse
    {
        public LiveStockSummary Summary { get; set; } = new LiveStockSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class TagCycleCountQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "CYCLE_COUNT";
        public string? SortDirection { get; set; } = "DESC";
    }

    public class TagCycleCountSummary
    {
        public int RecordCount { get; set; }
        public int Qty { get; set; }
        public double ExactAverage { get; set; }
        public int AvgTagPercentage { get; set; }
    }

    public class TagCycleCountResponse
    {
        public TagCycleCountSummary Summary { get; set; } = new TagCycleCountSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
        public List<Dictionary<string, object?>> Distribution { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class StoreDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "Store";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class StoreDashboardSummary
    {
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int HuReceivedQty { get; set; }
        public int HuValidatedQty { get; set; }
        public int HuWrongQty { get; set; }
        public int HhtValidateQty { get; set; }
        public int EncodedQty { get; set; }
        public string? StoreName { get; set; }
        public string? Date { get; set; }
    }

    public class StoreDashboardResponse
    {
        public StoreDashboardSummary Summary { get; set; } = new StoreDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class SaleDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "STORE";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class SaleDashboardSummary
    {
        public int RecordCount { get; set; }
        public int TotalDposSale { get; set; }
        public int TotalRfidCheckout { get; set; }
        public int TotalDposRfidSale { get; set; }
        public int TotalRfidCheckoutMatch { get; set; }
        public int TotalRfidCheckoutNotMatch { get; set; }
        public int TotalPosSaleNotMatch { get; set; }
        public int TotalTaffetaSale { get; set; }
        public int TotalManualSale { get; set; }
        public int TotalVoid { get; set; }
        public int TotalRfidCheckoutMatchDpos { get; set; }
        public int TotalDiffVoid { get; set; }
    }

    public class SaleDashboardResponse
    {
        public SaleDashboardSummary Summary { get; set; } = new SaleDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class ReturnDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "STORE";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class ReturnDashboardSummary
    {
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int ReturnQty { get; set; }
        public int ReturnEncodedQty { get; set; }
        public int PendingQty { get; set; }
    }

    public class ReturnDashboardResponse
    {
        public ReturnDashboardSummary Summary { get; set; } = new ReturnDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class VoidDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "STORE";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class VoidDashboardSummary
    {
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int ReturnQty { get; set; }
        public int ReturnEncodedQty { get; set; }
        public int PendingQty { get; set; }
    }

    public class VoidDashboardResponse
    {
        public VoidDashboardSummary Summary { get; set; } = new VoidDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class DcValidateDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "Store";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
    }

    public class DcValidateDashboardSummary
    {
        public int RecordCount { get; set; }
        public int ProcessedHu { get; set; }
        public int UnprocessedHu { get; set; }
        public int ArticleQty { get; set; }
    }

    public class DcValidateDashboardResponse
    {
        public DcValidateDashboardSummary Summary { get; set; } = new DcValidateDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class CycleCountDashboardQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? UserId { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "STORE CODE";
        public string? SortDirection { get; set; } = "ASC";
    }

    public class CycleCountDashboardSummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int RefNo { get; set; }
    }

    public class CycleCountDashboardResponse
    {
        public CycleCountDashboardSummary Summary { get; set; } = new CycleCountDashboardSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class VendorHUDiscrepancyQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "DIFF_TILL_DATE";
        public string? SortDirection { get; set; } = "asc";
        public string? SortType { get; set; } = "string";
        public string? UserId { get; set; } = string.Empty;
    }

    public class VendorHUDiscrepancySummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int ActualQty { get; set; }
        public int ScannedQty { get; set; }
        public int DifferenceQty { get; set; }
        public int DifferenceQtyTillDate { get; set; }
    }

    public class VendorHUDiscrepancyResponse
    {
        public VendorHUDiscrepancySummary Summary { get; set; } = new VendorHUDiscrepancySummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class TagManagementQueryRequest
    {
    }

    public class TagManagementSummary
    {
        public int RecordCount { get; set; }
        public int StoreCount { get; set; }
        public int WarehouseCount { get; set; }
    }

    public class TagManagementResponse
    {
        public TagManagementSummary Summary { get; set; } = new TagManagementSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class WarehouseEncodingQueryRequest
    {
        public string FromDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string ToDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }

    public class WarehouseEncodingSummary
    {
        public int Hour8To9 { get; set; }
        public int Hour9To10 { get; set; }
        public int Hour10To11 { get; set; }
        public int Hour11To12 { get; set; }
        public int Hour12To13 { get; set; }
        public int Hour13To14 { get; set; }
        public int Hour14To15 { get; set; }
        public int Hour15To16 { get; set; }
        public int Hour16To17 { get; set; }
        public int Hour17To18 { get; set; }
        public int Hour18To19 { get; set; }
        public int Hour19To20 { get; set; }
    }

    public class WarehouseEncodingResponse
    {
        public WarehouseEncodingSummary Summary { get; set; } = new WarehouseEncodingSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }
}
