namespace VS_Mart_Backend.Features.Reports.StockTake
{
    public class StockTakeModels
    {
    }

    public class StockTakeRequest
    {
        public string? UserId { get; set; }
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? StoreCode { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class StockTakeResponse
    {
        public object? Data { get; set; }

        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }

        public int NoOfArticles { get; set; }
        public int ActualQty { get; set; }
        public int SystemStock { get; set; }
        public int ScannedQty { get; set; }
        public int ScannedStock { get; set; }
        public int NetDifference { get; set; }
        public int DifferenceQty { get; set; }
        public int ShortQty { get; set; }
        public int ExcessQty { get; set; }
    }
}
