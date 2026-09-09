using System;
using System.Collections.Generic;

namespace VS_Mart_Backend.Features.CycleCountReport
{
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
}
