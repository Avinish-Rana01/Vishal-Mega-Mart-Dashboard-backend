using System;
using System.Collections.Generic;

namespace VS_Mart_Backend.Features.LiveStockReport
{
    public class StoreDropdownItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ArticleSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? StoreCode { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }

    public class ArticleItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class LiveStockReportRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? StoreName { get; set; }
        public string? StockDate { get; set; }
        public string? ArticleNo { get; set; }
        public string? SortColumn { get; set; } = "STOCK_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class LiveStockReportResponse
    {
        public ReportSummary Summary { get; set; } = new();
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class ReportSummary
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public int SapStockCount { get; set; }
        public int RfidStockCount { get; set; }
        public int DifferenceCount { get; set; }
        public string? StoreName { get; set; }
        public string? Date { get; set; }
    }
}
