namespace VS_Mart_Backend.Features.Dashboard.TagManagement
{
    public class TagManagementModels
    {
    }

    public class TagDetailsRequest
    {
        public string? SearchTerm { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public string? SortColumn { get; set; }

        public string? SortDirection { get; set; }
    }

    public class TagDetailsResponse
    {
        public List<TagDetailsData> TagData { get; set; } = new();

        public TagDetailsPager Pager { get; set; } = new();
    }

    public class TagDetailsPager
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int CycleCount { get; set; }
    }

    public class TagDetailsData
    {
        public string? STORE_NAME { get; set; }

        public int? CYCLE_COUNT { get; set; }
    }
}
