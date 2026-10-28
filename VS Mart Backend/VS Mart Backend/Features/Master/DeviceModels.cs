using System.Collections.Generic;

namespace VS_Mart_Backend.Features.Master
{
    public class DeviceItemDto
    {
        public int Device_ID { get; set; }
        public int? Store_ID { get; set; }
        public string Store_Name { get; set; } = string.Empty;
        public string Store_Code { get; set; } = string.Empty;
        public int? WH_ID { get; set; }
        public string Wh_Name { get; set; } = string.Empty;
        public string Wh_Code { get; set; } = string.Empty;
        public string Cash_Counter_No { get; set; } = string.Empty;
        public int? Device_Type_ID { get; set; }
        public string Device_Type_Name { get; set; } = string.Empty;
        public string Device_ESN { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string Created_Date { get; set; } = string.Empty;
        public int C_By { get; set; } = 1;
    }

    public class DeviceTypeDropdownDto
    {
        public int Device_Type_ID { get; set; }
        public string Device_Type_Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SaveDeviceRequest
    {
        public int Device_ID { get; set; } = 0;
        public int? Store_ID { get; set; }
        public int? WH_ID { get; set; }
        public string? Cash_Counter_No { get; set; }
        public int Device_Type_ID { get; set; }
        public string Device_ESN { get; set; } = string.Empty;
        public int User_ID { get; set; } = 1;
    }

    public class ToggleDeviceStatusRequest
    {
        public int Device_ID { get; set; }
    }

    public class DeviceDropdownsDto
    {
        public List<DeviceTypeDropdownDto> DeviceTypes { get; set; } = new();
        public List<dynamic> Stores { get; set; } = new();
        public List<dynamic> Warehouses { get; set; } = new();
    }
}

