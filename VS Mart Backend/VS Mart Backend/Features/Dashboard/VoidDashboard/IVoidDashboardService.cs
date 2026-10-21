using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.VoidDashboard
{
    public interface IVoidDashboardService
    {
        bool IsCacheEnabled();
        void SetCacheEnabled(bool enabled);
        
        Task<VoidDashboardResponse> GetVoidDashboardAsync(VoidDashboardQueryRequest request, CancellationToken cancellationToken = default);
        Task<VoidDetailsResponse> GetVoidDetailsAsync(VoidDetailsRequest request, CancellationToken cancellationToken = default);
        Task<VoidReconciliationResponse> GetVoidReconciliationDataAsync(VoidReconciliationRequest request, CancellationToken cancellationToken = default);
        Task<List<POSCounterResponse>> VoidBindPOSCounter(BindPOSCounterRequest request, CancellationToken cancellationToken = default);
        Task<List<EANItem>> SearchEAN(SearchEANRequest request, CancellationToken cancellationToken = default);
        Task<VoidReconciliationModelResponse> GetVoidReconciliationDataModelAsync(VoidReconciliationModelRequest request, CancellationToken cancellationToken = default);
    }
}
