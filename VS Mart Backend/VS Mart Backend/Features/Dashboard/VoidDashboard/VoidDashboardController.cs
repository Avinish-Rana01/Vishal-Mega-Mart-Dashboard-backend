using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace VS_Mart_Backend.Features.VoidDashboard
{
    [ApiController]
    [Route("api/Stock")]
    public class VoidDashboardController : ControllerBase
    {
        private readonly IVoidDashboardService _voidDashboardService;
        private readonly ILogger<VoidDashboardController> _logger;

        public VoidDashboardController(IVoidDashboardService voidDashboardService, ILogger<VoidDashboardController> logger)
        {
            _voidDashboardService = voidDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the void dashboard aggregated metrics.
        /// </summary>
        [HttpGet("void-dashboard")]
        public async Task<IActionResult> GetVoidDashboard([FromQuery] VoidDashboardQueryRequest query)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void dashboard data.");
                return StatusCode(500, new { message = "An error occurred while fetching void dashboard data." });
            }
        }

        [HttpGet("void/details")]
        [HttpGet("GetVoidDetails")]
        public async Task<IActionResult> GetVoidDetails([FromQuery] VoidDetailsRequest request)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void details.");
                return StatusCode(500, new { message = "An error occurred while fetching Void details." });
            }
        }

        [HttpGet("void/reconciliation")]
        [HttpGet("GetVoidReconciliationData")]
        public async Task<IActionResult> GetVoidReconciliationData([FromQuery] VoidReconciliationRequest request)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidReconciliationDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void reconciliation data.");
                return StatusCode(500, new { message = "An error occurred while fetching Void reconciliation data." });
            }
        }

        [HttpGet("void/pos-counters")]
        public async Task<IActionResult> VoidBindPOSCounter([FromQuery] BindPOSCounterRequest request)
        {
            try
            {
                var result = await _voidDashboardService.VoidBindPOSCounter(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void POS counters.");
                return StatusCode(500, new { message = "An error occurred while fetching POS counters." });
            }
        }

        [HttpGet("void/search-ean")]
        [HttpGet("void-SearchEAN")]
        public async Task<IActionResult> SearchEAN([FromQuery] SearchEANRequest request)
        {
            try
            {
                var result = await _voidDashboardService.SearchEAN(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void EANs.");
                return StatusCode(500, new { message = "An error occurred while fetching EANs." });
            }
        }

        [HttpGet("void/reconciliation-model")]
        [HttpGet("GetVoidReconciliationDataModel")]
        public async Task<IActionResult> GetVoidReconciliationDataModel([FromQuery] VoidReconciliationModelRequest request)
        {
            try
            {
                var result = await _voidDashboardService.GetVoidReconciliationDataModelAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching void reconciliation data model.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An internal server error occurred."
                });
            }
        }
    }
}
