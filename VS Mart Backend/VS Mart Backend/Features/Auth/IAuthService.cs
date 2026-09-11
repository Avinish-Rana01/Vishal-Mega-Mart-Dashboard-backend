using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        LoginResponse Login(LoginRequest request);
    }
}
