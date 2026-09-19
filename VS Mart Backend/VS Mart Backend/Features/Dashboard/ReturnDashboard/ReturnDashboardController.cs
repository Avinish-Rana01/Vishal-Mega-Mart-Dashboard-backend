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

        public ReturnDashboardController(IReturnDashboardService returnDashboardService)
        {
            _returnDashboardService = returnDashboardService;
        }

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
                return StatusCode(500, new { message = "An error occurred while fetching return details.", error = ex.Message });
            }
        }

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
                return StatusCode(500, new { message = "An error occurred while fetching return reconciliation data.", error = ex.Message });
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
                return StatusCode(500, new { message = "An error occurred while fetching Return POS counters.", error = ex.Message });
            }
        }

        [HttpGet("return-SearchEAN")]
        [HttpGet("return/search-ean")]
        public async Task<IActionResult> ReturnSearchEAN([FromQuery] VS_Mart_Backend.Features.VoidDashboard.SearchEANRequest request)
        {
            try
            {
                var result = await _returnDashboardService.ReturnSearchEAN(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Return EANs.", error = ex.Message });
            }
        }

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
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An internal server error occurred.",
                    Error = ex.Message
                });
            }
        }
    }
}

