using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.VoidDashboard
{
    public interface IVoidDashboardService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);
        
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request);
        Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request);
        Task<VoidReconciliationResponse> GetVoidReconciliationDataAsync(VoidReconciliationRequest request);
        Task<List<POSCounterResponse>> VoidBindPOSCounter(BindPOSCounterRequest request);
        Task<List<EANItem>> SearchEAN(SearchEANRequest request);
    }
}
