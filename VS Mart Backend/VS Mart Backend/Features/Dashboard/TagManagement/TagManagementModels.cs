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

        public List<TagStoreInventoryData> StoreInventory { get; set; } = new();

        public TagDetailsPager Pager { get; set; } = new();
    }

    public class TagDetailsPager
    {
        public int PageIndex { get; set; }

        public int RecordCount { get; set; }

        public int CycleCount { get; set; }

        public int StoreCount { get; set; }

        public int WhCount { get; set; }
    }

    public class TagStoreInventoryData
    {
        public string? STORE_NAME { get; set; }

        public int TAG_COUNT { get; set; }
    }

    public class TagDetailsData
    {
        public long? RowNumber { get; set; }

        public DateTime? DATE { get; set; }

        public string? TID { get; set; }

        public int? CYCLE_COUNT { get; set; }

        public string? LOCATION { get; set; }

        public string? LOCATION_NAME { get; set; }

        public string? STATUS { get; set; }

        // Backward compatibility & aliases
        public string? STORE_NAME { get => LOCATION_NAME; set => LOCATION_NAME = value; }

        public string? TAG_ID { get => TID; set => TID = value; }

        public int? RECYCLE_COUNT { get => CYCLE_COUNT; set => CYCLE_COUNT = value; }

        public string? TAG_LOCATION { get => LOCATION; set => LOCATION = value; }
    }
}
