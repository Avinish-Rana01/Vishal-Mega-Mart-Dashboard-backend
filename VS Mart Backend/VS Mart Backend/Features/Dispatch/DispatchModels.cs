using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace VS_Mart_Backend.Features.Dispatch
{
    public class DispatchUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
    }

    public class PicklistUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }

    public class DispatchUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int InsertedRows { get; set; }
        public int DuplicateRows { get; set; }
        public List<Dictionary<string, object>> Duplicates { get; set; } = new();
    }

    public class DispatchReportRequest
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortColumn { get; set; } = "TRANS_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class DispatchReportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new();
    }

    public class DispatchReportModalRequest
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string VehicleNo { get; set; } = string.Empty;
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortColumn { get; set; } = "TRANS_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class PicklistUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PicklistNo { get; set; }
        public string? Date { get; set; }
        public int TotalRecords { get; set; }
        public List<PicklistDataItem> Items { get; set; } = new();
    }

    public class PicklistDataItem
    {
        public string PicklistNo { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Qty { get; set; }
        public int Pack { get; set; }
        public int Box { get; set; }
    }
}
