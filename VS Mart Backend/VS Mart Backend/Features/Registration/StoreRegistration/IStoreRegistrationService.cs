using System.Collections.Generic;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.StoreRegistration
{
    public interface IStoreRegistrationService
    {
        Task<IEnumerable<StoreListItem>> GetAllStoresAsync();
        Task<IEnumerable<StoreDropdownItem>> GetStoreDropdownAsync(int userId = 0, string userType = "Super Admin");
        Task<ActionResponse> CreateStoreAsync(CreateStoreRequest request);
        Task<ActionResponse> UpdateStoreAsync(UpdateStoreRequest request);
        Task<ActionResponse> ToggleStoreStatusAsync(int storeId);
    }
}
