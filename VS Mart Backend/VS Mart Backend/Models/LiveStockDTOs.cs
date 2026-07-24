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
        public int TotalCount { get; set; }
        public int SapQty { get; set; }
        public int RfidQty { get; set; }
        public int DiffQty { get; set; }
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
}
