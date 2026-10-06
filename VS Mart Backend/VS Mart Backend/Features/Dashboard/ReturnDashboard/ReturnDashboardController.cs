using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.ReturnDashboard
{
    [ApiController]
    [Route("api/Stock")]
    public class ReturnDashboardController : ControllerBase
    {
        private readonly IReturnDashboardService _returnDashboardService;
        private readonly ILogger<ReturnDashboardController> _logger;

        public ReturnDashboardController(IReturnDashboardService returnDashboardService, ILogger<ReturnDashboardController> logger)
        {
            _returnDashboardService = returnDashboardService;
            _logger = logger;
        }

        [HttpGet("return/details")]
        [HttpGet("dashboard/return-details")]
        [HttpGet("GetReturnDetails")]
        public async Task<IActionResult> GetReturnDetails([FromQuery] ReturnDetailsRequest request)
        {
            try
            {
                var result = await _returnDashboardService.GetReturnDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching return details.");
                return StatusCode(500, new { message = "An error occurred while fetching return details." });
            }
        }

        [HttpGet("return/reconciliation")]
        [HttpGet("void/return-reconciliation")]
        [HttpGet("GetReturnReconciliationData")]
        public async Task<IActionResult> GetReturnReconciliationData([FromQuery] ReturnReconciliationRequest request)
        {
            try
            {
                var result = await _returnDashboardService.GetReturnReconciliationData(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching return reconciliation data.");
                return StatusCode(500, new { message = "An error occurred while fetching return reconciliation data." });
            }
        }

        [HttpGet("return/pos-counters")]
        [HttpGet("return-pos-counters")]
        public async Task<IActionResult> ReturnBindPOSCounter([FromQuery] VS_Mart_Backend.Features.VoidDashboard.BindPOSCounterRequest request)
        {
            try
            {
                var result = await _returnDashboardService.ReturnBindPOSCounter(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Return POS counters.");
                return StatusCode(500, new { message = "An error occurred while fetching Return POS counters." });
            }
        }

        [HttpGet("return/search-ean")]
        [HttpGet("return-SearchEAN")]
        public async Task<IActionResult> ReturnSearchEAN([FromQuery] VS_Mart_Backend.Features.VoidDashboard.SearchEANRequest request)
        {
            try
            {
                var result = await _returnDashboardService.ReturnSearchEAN(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Return EANs.");
                return StatusCode(500, new { message = "An error occurred while fetching Return EANs." });
            }
        }

        [HttpGet("return/reconciliation-model")]
        [HttpGet("GetReturnReconciliationDataModel")]
        public async Task<IActionResult> GetReturnReconciliationDataModel([FromQuery] ReturnReconciliationModelRequest request)
        {
            try
            {
                var result = await _returnDashboardService.GetReturnReconciliationDataModelAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Return Reconciliation Model.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An internal server error occurred."
                });
            }
        }
    }
}

