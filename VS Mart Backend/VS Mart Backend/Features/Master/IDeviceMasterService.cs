using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Master
{
    public interface IDeviceMasterService
    {
        Task<IEnumerable<DeviceItemDto>> GetDevicesAsync(int roleId = 4, int userId = 1, CancellationToken cancellationToken = default);
        Task<IEnumerable<DeviceTypeDropdownDto>> GetDeviceTypesAsync(CancellationToken cancellationToken = default);
        Task<DeviceDropdownsDto> GetDropdownsAsync(CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> SaveDeviceAsync(SaveDeviceRequest request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> ToggleStatusAsync(int deviceId, CancellationToken cancellationToken = default);
    }
}

