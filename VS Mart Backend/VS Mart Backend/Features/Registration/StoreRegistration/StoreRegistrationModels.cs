using System.ComponentModel.DataAnnotations;

namespace VS_Mart_Backend.Features.Registration.StoreRegistration
{
    public class StoreListItem
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class StoreDropdownItem
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }

    public class CreateStoreRequest
    {
        [Required(ErrorMessage = "Store code is required.")]
        [StringLength(10, ErrorMessage = "Store code cannot exceed 10 characters.")]
        public string StoreCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Store name is required.")]
        [StringLength(100, ErrorMessage = "Store name cannot exceed 100 characters.")]
        public string StoreName { get; set; } = string.Empty;

        public int EntryBy { get; set; } = 0;
    }

    public class UpdateStoreRequest
    {
        [Required(ErrorMessage = "Store ID is required.")]
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Store code is required.")]
        [StringLength(10, ErrorMessage = "Store code cannot exceed 10 characters.")]
        public string StoreCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Store name is required.")]
        [StringLength(100, ErrorMessage = "Store name cannot exceed 100 characters.")]
        public string StoreName { get; set; } = string.Empty;

        public int ModifyBy { get; set; } = 0;
    }
}
