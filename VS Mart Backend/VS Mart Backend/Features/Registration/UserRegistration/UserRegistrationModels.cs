using System.ComponentModel.DataAnnotations;

namespace VS_Mart_Backend.Features.Registration.UserRegistration
{
    public class UserListItem
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UserRoleItem
    {
        public string UserType { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        [Required(ErrorMessage = "User name is required.")]
        [StringLength(20, ErrorMessage = "User name cannot exceed 20 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(20, ErrorMessage = "Password cannot exceed 20 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "User type is required.")]
        [StringLength(20, ErrorMessage = "User type cannot exceed 20 characters.")]
        public string UserType { get; set; } = string.Empty;

        public int StoreId { get; set; } = 0;
        public int WhId { get; set; } = 0;
        public int EntryBy { get; set; } = 0;
    }

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(20, ErrorMessage = "User name cannot exceed 20 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(20, ErrorMessage = "Password cannot exceed 20 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "User type is required.")]
        [StringLength(20, ErrorMessage = "User type cannot exceed 20 characters.")]
        public string UserType { get; set; } = string.Empty;

        public int StoreId { get; set; } = 0;
        public int WhId { get; set; } = 0;
        public int ModifyBy { get; set; } = 0;
    }
}
