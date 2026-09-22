namespace VS_Mart_Backend.Features.Reports
{
    public class TagCleaningReportsModels
    {
    }

    public class TagCleaningReportRequest
    {

        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
        public string SearchTerm { get; set; } = "";

        // Required only for TAG_CLEANING_REPORT
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string SortColumn { get; set; } = "INWARD_DATE";
        public string SortDirection { get; set; } = "ASC";
    }

    

    public class TagCleaningReportResponse
    {
        public string Action { get; set; } = "";

        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int TagValidatedCount { get; set; }

        public List<TagCleaningReportModel> Data { get; set; } = new();
    }
    public class TagCleaningReportModel
    {
        public long? SR_NO { get; set; }
        public DateTime? INWARD_DATE { get; set; }
        public string? STORE_NAME { get; set; }
        public int? TOTAL_COUNT { get; set; }
    }

    
}
