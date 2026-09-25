namespace VS_Mart_Backend.Features.Dashboard.DCEncoding
{
    public interface IWHEncodingDetails
    {
        Task<WHEncodingResponse> GetWHEncodingDetailsAsync(WHEncodingRequest request);

        Task<List<Username>> SearchUsernameAsync(UsernameRequest request);
    }
}
