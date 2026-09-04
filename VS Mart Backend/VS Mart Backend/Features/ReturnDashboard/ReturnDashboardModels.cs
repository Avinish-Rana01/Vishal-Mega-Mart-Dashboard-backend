using System.Collections.Generic;

namespace VS_Mart_Backend.Features.ReturnDashboard
{
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
        public string? SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";

        public string? Ean { get; set; } = "";
        public string? StoreName { get; set; } = "";
        public string? Pos { get; set; } = "";

        public string? SortColumn { get; set; } = "";
        public string? SortDirection { get; set; } = "";
    }

    public class ReturnReconciliationResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int ReturnQty { get; set; }
        public int EncodeQty { get; set; }
        public int DifferenceQty { get; set; }

        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }
}

