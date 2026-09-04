using System;
using System.Collections.Generic;

namespace VS_Mart_Backend.Features.VoidDashboard
{
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

    public class BindPOSCounterRequest
    {
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? ColumnName { get; set; } = string.Empty;
        public string? Store { get; set; } = string.Empty;
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
}
