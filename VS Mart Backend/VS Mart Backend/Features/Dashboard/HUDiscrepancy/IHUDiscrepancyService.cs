using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    public interface IHUDiscrepancyService
    {
        Task<VendorWiseHUDiscrepancyResponse> GetVendorHUDiscrepancyDataAsync(VendorHUDiscrepancyRequest request);
    }
}
