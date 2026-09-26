using System;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    public class UniversalExportRequest
    {
        public string ReportName { get; set; } = string.Empty;
        public string? SearchTerm { get; set; }
        public string? StoreCode { get; set; }
        public string? StoreName { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? VendorCode { get; set; }
        public string? HuNo { get; set; }
        public string? Ean { get; set; }
        public string? Pos { get; set; }
        public string? ArticleNo { get; set; }
        public string? Material { get; set; }
        public string? RefNo { get; set; }
        public string? User { get; set; }
        public int? UserId { get; set; }
        public string? GrcStatus { get; set; }
        public string? HuStatus { get; set; }
        public string? ReceivingPlant { get; set; }
        public string? ColumnName { get; set; }
        public string? ScanTime { get; set; }
        public string? Article { get; set; }
        public string? BillDate { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
        public string? Format { get; set; } = "xlsx";
    }
}
