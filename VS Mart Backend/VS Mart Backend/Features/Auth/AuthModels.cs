namespace VS_Mart_Backend.Features.Auth
{
    public class LoginRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

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
        public List<string> AllowedSections { get; set; } = new();
        public bool RequirePasswordChange { get; set; } = false;
        public string? IsLoginStatus { get; set; } = "1";
    }

    public class ChangePasswordRequest
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public class ChangePasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
