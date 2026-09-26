using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    public interface IUniversalExportService
    {
        Task<bool> StreamExportAsync(UniversalExportRequest request, Stream outputStream, CancellationToken cancellationToken);
    }
}
