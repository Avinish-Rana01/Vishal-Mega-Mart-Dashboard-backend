using System.ComponentModel.DataAnnotations;

namespace VS_Mart_Backend.Features.Registration.WarehouseRegistration
{
    public class WarehouseListItem
    {
        public int WhId { get; set; }
        public string WhCode { get; set; } = string.Empty;
        public string WhName { get; set; } = string.Empty;
        public string WhAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class WarehouseDropdownItem
    {
        public int WhId { get; set; }
        public string WhName { get; set; } = string.Empty;
    }

    public class CreateWarehouseRequest
    {
        [Required(ErrorMessage = "Warehouse code is required.")]
        [StringLength(30, ErrorMessage = "Warehouse code cannot exceed 30 characters.")]
        public string WhCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Warehouse name is required.")]
        [StringLength(50, ErrorMessage = "Warehouse name cannot exceed 50 characters.")]
        public string WhName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Warehouse address cannot exceed 200 characters.")]
        public string WhAddress { get; set; } = string.Empty;

        public int EntryBy { get; set; } = 0;
    }

    public class UpdateWarehouseRequest
    {
        [Required(ErrorMessage = "Warehouse ID is required.")]
        public int WhId { get; set; }

        [Required(ErrorMessage = "Warehouse code is required.")]
        [StringLength(30, ErrorMessage = "Warehouse code cannot exceed 30 characters.")]
        public string WhCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Warehouse name is required.")]
        [StringLength(50, ErrorMessage = "Warehouse name cannot exceed 50 characters.")]
        public string WhName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Warehouse address cannot exceed 200 characters.")]
        public string WhAddress { get; set; } = string.Empty;

        public int ModifyBy { get; set; } = 0;
    }
}
