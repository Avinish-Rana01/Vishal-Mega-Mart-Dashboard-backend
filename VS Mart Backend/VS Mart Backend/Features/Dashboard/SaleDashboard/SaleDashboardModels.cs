namespace VS_Mart_Backend.Features.SaleDashboard
{
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
}
