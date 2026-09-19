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

    public class ReturnReconciliationModelRequest
    {
        public string? SearchTerm { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? BillDate { get; set; } = string.Empty;
        public string? StoreCode { get; set; } = string.Empty;
        public string? Pos { get; set; } = string.Empty;
        public string? Ean { get; set; } = string.Empty;
        public string? SortColumn { get; set; } = "BILL_DATE";
        public string? SortDirection { get; set; } = "ASC";
    }

    public class ReturnReconciliationModel
    {
        public DateTime? BILL_DATE { get; set; }
        public string POS_TYPE { get; set; } = string.Empty;
        public string STORE_CODE { get; set; } = string.Empty;
        public string COUNTER_NO { get; set; } = string.Empty;
        public string EAN { get; set; } = string.Empty;
        public string MATERIAL { get; set; } = string.Empty;
        public int RETURN_QTY { get; set; }
        public int ENCODE_QTY { get; set; }
        public int DIFFERENCE_QTY { get; set; }
        public string STATUS { get; set; } = string.Empty;
    }

    public class ReturnReconciliationModelResponse
    {
        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int ReturnQty { get; set; }
        public int EncodeQty { get; set; }
        public int DifferenceQty { get; set; }
        public IEnumerable<ReturnReconciliationModel> Data { get; set; } = new List<ReturnReconciliationModel>();
    }
}

