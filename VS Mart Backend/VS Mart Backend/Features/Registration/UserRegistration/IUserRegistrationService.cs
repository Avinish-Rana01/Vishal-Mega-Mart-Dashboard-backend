using System.Collections.Generic;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.UserRegistration
{
    public interface IUserRegistrationService
    {
        Task<IEnumerable<UserListItem>> GetUsersAsync(int userId = 0, string userType = "Super Admin");
        Task<IEnumerable<string>> GetUserRolesAsync();
        Task<ActionResponse> CreateUserAsync(CreateUserRequest request);
        Task<ActionResponse> UpdateUserAsync(UpdateUserRequest request);
        Task<ActionResponse> ToggleUserStatusAsync(int userId);
    }
}
