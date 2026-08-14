namespace VS_Mart_Backend.Models
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

    public class LiveStockReportQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = string.Empty;
        public string? SortDirection { get; set; } = "asc";
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? ArticleNo { get; set; } = string.Empty;
        public string? StoreName { get; set; } = string.Empty;
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

    public class StoreGrcReportQueryRequest
    {
        public string? StoreCode { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "DATE";
        public string? SortDirection { get; set; } = "DESC";
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

    public class CycleCountReportViewQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "DATE";
        public string? SortDirection { get; set; } = "DESC";
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? StoreCode { get; set; } = string.Empty;
    }

    public class CycleCountReportViewSummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int RefNo { get; set; }
    }

    public class CycleCountReportViewResponse
    {
        public CycleCountReportViewSummary Summary { get; set; } = new CycleCountReportViewSummary();
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

    // --- Cycle Count Details DTOs ---
    public class CycleCountDetailsQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "STORE_CODE";
        public string? SortDirection { get; set; } = "asc";
        public string? StoreCode { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? RefNo { get; set; } = string.Empty;
    }

    public class CycleCountDetailsSummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int ActualQty { get; set; }
        public int ScannedQty { get; set; }
        public int DiffQty { get; set; }
        public int ExcessQty { get; set; }
    }

    public class CycleCountDetailsResponse
    {
        public CycleCountDetailsSummary Summary { get; set; } = new CycleCountDetailsSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    // --- Vendor HU Discrepancy Dashboard DTOs ---

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

    // --- Tag Management Location DTOs ---

    public class TagManagementQueryRequest
    {
        // No pagination or sorting requested by frontend for this specific chart
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

    // --- Warehouse Encoding Details DTOs ---

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

    // --- Store Sale Report DTOs ---

    public class StoreSaleReportQueryRequest
    {
        public string? UserId { get; set; }
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? StoreCode { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class StoreSaleReportSummary
    {
        public int RecordCount { get; set; }
        public int POSSaleQty { get; set; }
        public int RFIDCheckoutQty { get; set; }
        public int TaffetaSaleQty { get; set; }
        public int ManualSaleQty { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
    }

    public class StoreSaleReportResponse
    {
        public StoreSaleReportSummary Summary { get; set; } = new StoreSaleReportSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    // --- Sale Details Dropdowns & Grid DTOs ---

    public class DropdownItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class BindPOSCounterRequest
    {
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? ColumnName { get; set; } = string.Empty;
        public string? Store { get; set; } = string.Empty;
    }

    public class SearchArticlesSaleRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public string? Store { get; set; } = string.Empty;
        public string? ColumnName { get; set; } = string.Empty;
        public string? Pos { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
    }

    public class SearchEANSaleRequest : SearchArticlesSaleRequest
    {
        public string? Material { get; set; } = string.Empty;
    }

    public class SaleDataQueryRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "ITEM_CD";
        public string? SortDirection { get; set; } = "asc";
        public string? ColumnName { get; set; } = string.Empty;
        public string? StoreName { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? Pos { get; set; } = string.Empty;
        public string? ArticleNo { get; set; } = string.Empty;
        public string? Ean { get; set; } = string.Empty;
        public string? UserId { get; set; } = string.Empty;
    }

    public class SaleDataSummary
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int Qty { get; set; }
    }

    public class SaleDataResponse
    {
        public SaleDataSummary Summary { get; set; } = new SaleDataSummary();
        public List<Dictionary<string, object?>> Items { get; set; } = new List<Dictionary<string, object?>>();
    }

    public class VoidDetailsRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "DATE";
        public string? SortDirection { get; set; } = "asc";
        public string? ColumnName { get; set; } = string.Empty;
        public string? StoreName { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
    }

    public class VoidDetailsResponse
    {
        public object? Data { get; set; }

        public int? PageIndex { get; set; }

        public int? RecordCount { get; set; }

        public int? VoidQty { get; set; }

        public int? EncodeQty { get; set; }

        public int? DifferenceQty { get; set; }
    }

    public class VoidReconciliationRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "DATE";
        public string? SortDirection { get; set; } = "asc";
        public string? StoreName { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? pos { get; set; } = string.Empty;
        public string? Ean { get; set; } = string.Empty;
    }

    public class VoidReconciliationResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int VoidQty { get; set; }
        public int EncodeQty { get; set; }
        public int DifferenceQty { get; set; }
        public object? Data { get; set; }

      
    }

    public class POSCounterResponse
    {
        public string? id { get; set; }
        public string? text { get; set; }
    }

    public class EANItem
    {
        public string? id { get; set; }
        public string? text { get; set; }
    }

    public class SearchEANRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public string? Store { get; set; } = string.Empty;
        public string? Pos { get; set; } = string.Empty;
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
    }

    public class ReturnDetailsRequest
    {
        public string? SearchTerm { get; set; } = "";
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }

        public string? StoreName { get; set; } = "";
        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";

        public string? SortColumn { get; set; } = "";
        public string? SortDirection { get; set; } = "";
    }

    public class ReturnDetailsResponse
    {
        public int? PageIndex { get; set; }
        public int? RecordCount { get; set; }
        public int? ReturnQty { get; set; }
        public int? EncodeQty { get; set; }
        public int? DifferenceQty { get; set; }

        public object? Data { get; set; }
    }

    public class ReturnReconciliationRequest
    {
        public string SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";

        public string Ean { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string Pos { get; set; } = "";

        public string SortColumn { get; set; } = "";
        public string SortDirection { get; set; } = "";
    }

    public class ReturnReconciliationResponse
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int ReturnQty { get; set; }

        public int EncodeQty { get; set; }

        public int DifferenceQty { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; }
            = new();
    }

    public class DCDetailsRequest
    {
        public string SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string StoreName { get; set; } = "";
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";

        public string SortColumn { get; set; } = "";
        public string SortDirection { get; set; } = "";
    }

    public class DCDetailsResponse
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int ProcessedCount { get; set; }

        public int UnprocessedCount { get; set; }

        public int ValidatedCount { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; }
            = new();
    }

    public class HUDetailsRequest
    {
        public string SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string HUStatus { get; set; } = "";
        public string HUNo { get; set; } = "";

        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";

        public string ReceivingPlant { get; set; } = "";

        public string SortColumn { get; set; } = "";
        public string SortDirection { get; set; } = "";
    }

    public class HUDetailsResponse
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int MaterialQty { get; set; }

        public int ActualQty { get; set; }

        public int ScannedQty { get; set; }

        public int InvalidTags { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; }
            = new();
    }

    public class EncodingStoreDataRequest
    {
        public string SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";

        public string Ean { get; set; } = "";
        public string ArticleNo { get; set; } = "";
        public string StoreName { get; set; } = "";
        public int? UserId { get; set; } = 0;

        public string SortColumn { get; set; } = "";
        public string SortDirection { get; set; } = "";
    }

    public class EncodingStoreDataResponse
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int TotalCount { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; }
            = new();
    }
}

