using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace VS_Mart_Backend.Features.VoidDashboard
{
    [ApiController]
    public class VoidDashboardController : ControllerBase
    {
        private readonly IVoidDashboardService _voidDashboardService;

        public VoidDashboardController(IVoidDashboardService voidDashboardService)
        {
            _voidDashboardService = voidDashboardService;
        }

        /// <summary>
        /// Retrieves the void dashboard aggregated metrics.
        /// </summary>
        [HttpGet("/api/Stock/void-dashboard")]
        public async Task<IActionResult> GetVoidDashboard([FromQuery] VoidDashboardQueryRequest query)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching void dashboard data.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/GetVoidDetails")]
        public async Task<IActionResult> GetVoidDetails([FromQuery] VoidDetailsRequest request)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Void details.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/GetVoidReconciliationData")]
        public async Task<IActionResult> GetVoidReconciliationData([FromQuery] VoidReconciliationRequest request)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidReconciliationDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Void reconciliation data.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/void/pos-counters")]
        public async Task<IActionResult> VoidBindPOSCounter([FromQuery] BindPOSCounterRequest request)
        {
            try
            {
                var result = await _voidDashboardService.VoidBindPOSCounter(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching POS counters.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/void-SearchEAN")]
        public async Task<IActionResult> SearchEAN([FromQuery] SearchEANRequest request)
        {
            try
            {
                var result = await _voidDashboardService.SearchEAN(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching EANs.", error = ex.Message });
            }
        }
    }
}
