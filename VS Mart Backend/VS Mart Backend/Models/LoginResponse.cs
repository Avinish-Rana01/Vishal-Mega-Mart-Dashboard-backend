namespace VS_Mart_Backend.Models
{
    public class LoginResponse
    {
        public bool? Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectPage { get; set; }

        public string? UserName { get; set; }
        public string? UserID { get; set; }
        public string? UserType { get; set; }
        public string? StoreName { get; set; }
        public string? WarehouseName { get; set; }
        public string? StoreCode { get; set; }
        public string? WarehouseCode { get; set; }
    }
}
