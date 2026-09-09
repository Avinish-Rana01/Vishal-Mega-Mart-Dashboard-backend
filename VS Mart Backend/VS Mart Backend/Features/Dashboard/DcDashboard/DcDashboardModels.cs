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
}

