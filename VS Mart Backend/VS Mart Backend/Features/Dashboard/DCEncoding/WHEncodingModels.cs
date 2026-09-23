namespace VS_Mart_Backend.Features.Dashboard.DCEncoding
{
    public class WHEncodingModels
    {
    }

    public class WHEncodingRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int? User { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class WHEncodingResponse
    {
        public object? Data { get; set; }

        public int PageIndex { get; set; }
        public int RecordCount { get; set; }
        public int TotalCount { get; set; }
        public int UserCount { get; set; }

        public int C8TO9 { get; set; }
        public int C9TO10 { get; set; }
        public int C10TO11 { get; set; }
        public int C11TO12 { get; set; }
        public int C12TO13 { get; set; }
        public int C13TO14 { get; set; }
        public int C14TO15 { get; set; }
        public int C15TO16 { get; set; }
        public int C16TO17 { get; set; }
        public int C17TO18 { get; set; }
        public int C18TO19 { get; set; }
        public int C19TO20 { get; set; }

        public int C8TO9_ERR { get; set; }
        public int C9TO10_ERR { get; set; }
        public int C10TO11_ERR { get; set; }
        public int C11TO12_ERR { get; set; }
        public int C12TO13_ERR { get; set; }
        public int C13TO14_ERR { get; set; }
        public int C14TO15_ERR { get; set; }
        public int C15TO16_ERR { get; set; }
        public int C16TO17_ERR { get; set; }
        public int C17TO18_ERR { get; set; }
        public int C18TO19_ERR { get; set; }
        public int C19TO20_ERR { get; set; }

        public int MRGQTY { get; set; }
        public int EVNQTY { get; set; }
        public int ENCQTY { get; set; }
        public int AVGQTY { get; set; }

        public int T_ENC_QTY { get; set; }
        public int T_ENC_USERS { get; set; }
        public int ERRORQTY { get; set; }
    }
}
