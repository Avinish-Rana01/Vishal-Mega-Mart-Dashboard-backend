using System.Collections.Generic;

namespace VS_Mart_Backend.Features.DcDashboard
{
    public class DCDetailsRequest
    {
        public string? SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string? StoreName { get; set; } = "";
        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";

        public string? SortColumn { get; set; } = "";
        public string? SortDirection { get; set; } = "";
    }

    public class DCDetailsResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int ProcessedCount { get; set; }
        public int UnprocessedCount { get; set; }
        public int ValidatedCount { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class HUDetailsRequest
    {
        public string? SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string? HUStatus { get; set; } = "";
        public string? HUNo { get; set; } = "";
        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";
        public string? ReceivingPlant { get; set; } = "";
        public string? SortColumn { get; set; } = "";
        public string? SortDirection { get; set; } = "";
    }

    public class HUDetailsResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int MaterialQty { get; set; }
        public int ActualQty { get; set; }
        public int ScannedQty { get; set; }
        public int InvalidTags { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class HuNumberItem
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class HUReportViewRequest
    {
        public string? SearchTerm { get; set; } = "";
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? HUStatus { get; set; } = "";
        public string? HUNo { get; set; } = "";
        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";
        public string? RefNo { get; set; } = "";
        public string? SortColumn { get; set; } = "HU_Number";
        public string? SortDirection { get; set; } = "asc";
    }

    public class HUReportViewResponse
    {
        public int PageIndex { get; set; } = 1;
        public int RecordCount { get; set; }
        public int ActualQty { get; set; }
        public int ScannedQty { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }
}


