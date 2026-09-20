using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.DcDashboard
{
    [ApiController]
    [Route("api/Stock")]
    public class DcDashboardController : ControllerBase
    {
        private readonly IDcDashboardService _dcDashboardService;

        public DcDashboardController(IDcDashboardService dcDashboardService)
        {
            _dcDashboardService = dcDashboardService;
        }

        [HttpGet("GetDCDetails")]
        public async Task<IActionResult> GetDCDetails([FromQuery] DCDetailsRequest request)
        {
            try
            {
                var result = await _dcDashboardService.GetDCDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching DC details.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("Hu-details")]
        public async Task<IActionResult> GetHUDetails([FromQuery] HUDetailsRequest request)
        {
            try
            {
                var result = await _dcDashboardService.GetHUDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching HU details.", error = ex.Message });
            }
        }

        [HttpGet("hu-numbers/search")]
        public async Task<IActionResult> SearchValidationHuNumbers(
            [FromQuery] string? huStatus, 
            [FromQuery] string? receivingPlant, 
            [FromQuery] string? fromDate, 
            [FromQuery] string? toDate, 
            [FromQuery] string? searchTerm)
        {
            try
            {
                var result = await _dcDashboardService.SearchValidationHuNumbersAsync(huStatus, receivingPlant, fromDate, toDate, searchTerm);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching validation HU numbers.", error = ex.Message });
            }
        }
    }
}

