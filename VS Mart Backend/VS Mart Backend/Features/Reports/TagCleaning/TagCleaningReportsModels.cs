namespace VS_Mart_Backend.Features.Reports
{
    public class TagCleaningReportRequest
    {
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
        public string SearchTerm { get; set; } = "";

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string SortColumn { get; set; } = "TAG_CLEANED_DATE";
        public string SortDirection { get; set; } = "DESC";
    }

    public class TagCleaningReportResponse
    {
        public string Action { get; set; } = "TAG_CLEANING_CONSOLIDATE_REPORT";

        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int TotalValidatedCount { get; set; }
        public int TotalCleanedCount { get; set; }

        public List<TagCleaningReportModel> Data { get; set; } = new();
    }

    public class TagCleaningReportModel
    {
        public long? RowNumber { get; set; }
        public long? SR_NO => RowNumber;
        public DateTime? TAG_CLEANED_DATE { get; set; }
        public int? TOTAL_VALIDATED_QTY { get; set; }
        public int? TOTAL_CLEANED_QTY { get; set; }
    }
}
