namespace VS_Mart_Backend.Features.Dashboard.TagManagement
{
    public interface ITagManagement
    {
        Task<TagDetailsResponse> GetTagDetailsAsync(TagDetailsRequest request);
    }
}
