namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    public class VendorHUDiscrepancyRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string? VendorCode { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class VendorWiseHUDiscrepancyResponse
    {
        public List<Dictionary<string, object?>> Data { get; set; } = new();

        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int ActualQty { get; set; }
        public int ScannedQty { get; set; }
        public int DifferenceQty { get; set; }
        public int ExcessQty { get; set; }
        public int HUCount { get; set; }
    }

    public class StoreUserRequest
    {
        public string? Status { get; set; }
        public int Entry_By { get; set; }
        public int Modify_By { get; set; }
        public int Store_ID { get; set; }
        public string Store_Code { get; set; }
        public string Store_Name { get; set; }
        public string MAIL_ID { get; set; }
        public int State_ID { get; set; }
        public int City_ID { get; set; }
        public int SM_ID { get; set; }
        public int AM_ID { get; set; }
        public int ZFM_ID { get; set; }
        public int LP_ID { get; set; }
        public int User_ID { get; set; }
        public string User_Name { get; set; }
        public string Password { get; set; }
        public string User_Type { get; set; }
        public int WH_ID { get; set; }
        public int Role_ID { get; set; }
        public int Emp_ID { get; set; }
        public int Reader_Config_ID { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Data { get; set; }
    }
}
