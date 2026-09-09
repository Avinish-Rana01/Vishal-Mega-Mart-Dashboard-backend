using System.Collections.Generic;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.WarehouseRegistration
{
    public interface IWarehouseRegistrationService
    {
        Task<IEnumerable<WarehouseListItem>> GetAllWarehousesAsync();
        Task<IEnumerable<WarehouseDropdownItem>> GetWarehouseDropdownAsync(int userId = 0, string userType = "Super Admin");
        Task<ActionResponse> CreateWarehouseAsync(CreateWarehouseRequest request);
        Task<ActionResponse> UpdateWarehouseAsync(UpdateWarehouseRequest request);
        Task<ActionResponse> ToggleWarehouseStatusAsync(int whId);
    }
}
