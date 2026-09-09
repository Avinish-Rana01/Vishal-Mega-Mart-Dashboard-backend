using System.Collections.Generic;

namespace VS_Mart_Backend.Features.Master
{
    public class MasterRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Store_Code { get; set; } = string.Empty;
        public string? Store_Name { get; set; } = string.Empty;
        public int Entry_By { get; set; } = 0;
        public int Is_Status { get; set; } = 0;
        public int Reader_Id { get; set; } = 0;
        public string? Reader_Name { get; set; } = string.Empty;
        public string? Reader_MAC { get; set; } = string.Empty;
        public int Antena { get; set; } = 0;
        public string? User_Name { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? User_Type { get; set; } = string.Empty;
        public int Reader_Config_ID { get; set; } = 0;
        public int Modify_By { get; set; } = 0;
        public int Store_ID { get; set; } = 0;
        public int User_ID { get; set; } = 0;
        public string? Device_ESN { get; set; } = string.Empty;
        public string? Encode_DateTime { get; set; } = string.Empty;
        public int WH_ID { get; set; } = 0;
        public string? Wh_Code { get; set; } = string.Empty;
        public string? Wh_Name { get; set; } = string.Empty;
        public string? Wh_Address { get; set; } = string.Empty;
    }

    public class MasterResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public IEnumerable<dynamic>? Data { get; set; }
    }
}
