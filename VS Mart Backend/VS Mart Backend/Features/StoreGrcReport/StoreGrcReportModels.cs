using System.Collections.Generic;

namespace VS_Mart_Backend.Features.StoreGrcReport
{
    public class HuNumberItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class GrcHuSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? GrcStatus { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? StoreCode { get; set; }
    }

    public class GrcDetailsRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? GrcStatus { get; set; }
        public string? StoreName { get; set; }
        public string? HuNo { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SortColumn { get; set; } = "GRC_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class GrcDetailsResponse
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class GrcModalDetailsRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "GRC_DATE";
        public string? SortDirection { get; set; } = "asc";
        public string? Date { get; set; }
        public string? StoreCode { get; set; }
        public string? HuNumber { get; set; }
        public string? GrcStatus { get; set; }
    }

    public class GrcModalDetailsResponse
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public int Qty { get; set; }
        public int MaterialCount { get; set; }
        public int ActualQty { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
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
}
