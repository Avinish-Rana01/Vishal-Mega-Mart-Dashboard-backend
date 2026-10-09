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
        public int Store_Floor_ID { get; set; } = 0;
        public string? Store_Floor { get; set; } = string.Empty;
        public string? State { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string? Store_Manager { get; set; } = string.Empty;
        public string? StoreManager { get => Store_Manager; set => Store_Manager = value; }
        public string? Area_Manager { get; set; } = string.Empty;
        public string? AreaManager { get => Area_Manager; set => Area_Manager = value; }
        public string? ZFM { get; set; } = string.Empty;
        public string? LP { get; set; } = string.Empty;
        public string? Email_ID { get; set; } = string.Empty;
        public string? EmailId { get => Email_ID; set => Email_ID = value; }
        public string? Email { get => Email_ID; set => Email_ID = value; }
        public bool Is_Email_Required { get; set; } = false;
        public bool IsEmailRequired { get => Is_Email_Required; set => Is_Email_Required = value; }

        // Parameters for New UM DB schema (SP_Master)
        public int State_ID { get; set; } = 0;
        public int City_ID { get; set; } = 0;
        public string? State_Name { get; set; } = string.Empty;
        public int SM_ID { get; set; } = 0;
        public int AM_ID { get; set; } = 0;
        public int ZFM_ID { get; set; } = 0;
        public int LP_ID { get; set; } = 0;
        public string? MAIL_ID { get; set; } = string.Empty;
        public int Email_Required_Flag { get; set; } = 0;
        public int Role_ID { get; set; } = 0;
        public int Emp_ID { get; set; } = 0;
        public string? Emp_Code { get; set; } = string.Empty;
        public string? Emp_Name { get; set; } = string.Empty;
        public string? RoleName { get; set; } = string.Empty;
        public string? CounterRoleid { get; set; } = string.Empty;
        public int CounterEmpID { get; set; } = 0;
        public int CounterStoreID { get; set; } = 0;
    }

    public class MasterResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public IEnumerable<dynamic>? Data { get; set; }
    }

    public class StoreDropdownOptionsDto
    {
        public List<string> States { get; set; } = new();
        public List<StateCityDto> Cities { get; set; } = new();
        public List<string> StoreManagers { get; set; } = new();
        public List<string> AreaManagers { get; set; } = new();
        public List<string> ZFMs { get; set; } = new();
        public List<string> LPs { get; set; } = new();
    }

    public class StateCityDto
    {
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}

