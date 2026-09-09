using System.Collections.Generic;

namespace VS_Mart_Backend.Features.SystemUtility
{

    public class EncodingStoreDataRequest
    {
        public string? SearchTerm { get; set; } = "";
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string? FromDate { get; set; } = "";
        public string? ToDate { get; set; } = "";

        public string? Ean { get; set; } = "";
        public string? ArticleNo { get; set; } = "";
        public string? StoreName { get; set; } = "";
        public int? UserId { get; set; } = 0;

        public string? SortColumn { get; set; } = "";
        public string? SortDirection { get; set; } = "";
    }

    public class EncodingStoreDataResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }
}
