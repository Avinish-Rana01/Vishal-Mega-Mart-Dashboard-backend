namespace VS_Mart_Backend.Features.Auth
{
    public interface IAuthService
    {
        LoginResponse Login(LoginRequest request);
    }
}
