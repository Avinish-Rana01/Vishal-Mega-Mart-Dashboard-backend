using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dispatch
{
    public interface IDispatchService
    {
        Task<DispatchUploadResponse> ProcessUploadAsync(IFormFile file, string status, CancellationToken cancellationToken = default);
        Task<DispatchReportResponse> GetDispatchReportAsync(DispatchReportRequest request, CancellationToken cancellationToken = default);
        Task<DispatchReportResponse> GetDispatchReportModalAsync(DispatchReportModalRequest request, CancellationToken cancellationToken = default);
        Task<PicklistUploadResponse> ProcessPicklistUploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
