using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Master
{
    public interface IMasterService
    {
        Task<MasterResponse> ExecuteMasterAsync(MasterRequest request);
    }
}
