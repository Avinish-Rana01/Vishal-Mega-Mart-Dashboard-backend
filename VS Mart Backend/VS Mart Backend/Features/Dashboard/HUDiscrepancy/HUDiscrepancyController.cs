using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    [ApiController]
    [Route("api/[controller]")]
    public class HUDiscrepancyController : ControllerBase
    {
        private readonly IHUDiscrepancyService _huDiscrepancyService;
        private readonly ILogger<HUDiscrepancyController> _logger;

        public HUDiscrepancyController(IHUDiscrepancyService huDiscrepancyService, ILogger<HUDiscrepancyController> logger)
        {
            _huDiscrepancyService = huDiscrepancyService;
            _logger = logger;
        }

        [HttpGet("GetVendorHUDiscrepancyData")]
        public async Task<IActionResult> GetVendorHUDiscrepancyData([FromQuery] VendorHUDiscrepancyRequest request)
        {
            try
            {
                var result = await _huDiscrepancyService.GetVendorHUDiscrepancyDataAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GetVendorHUDiscrepancyData.");
                return StatusCode(500, "An error occurred while loading stores.");
            }
        }
    }
}
