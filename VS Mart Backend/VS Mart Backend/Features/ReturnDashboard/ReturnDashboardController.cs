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
    }
}

