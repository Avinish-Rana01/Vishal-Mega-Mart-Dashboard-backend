using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VS_Mart_Backend.Features.LiveStockReport;

namespace VS_Mart_Backend.Features.HUDiscrepancy
{
    public class HUDiscrepancyController : ControllerBase
    {

        private readonly IHUDiscrepancyService _huDiscrepancyService;
        private readonly ILogger<LiveStockReportController> _logger;

        public HUDiscrepancyController(IHUDiscrepancyService huDiscrepancyService, ILogger<LiveStockReportController> logger)
        {
            _huDiscrepancyService = huDiscrepancyService;
            _logger = logger;
        }

        [HttpPost("GetVendorHUDiscrepancyData")]
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

        [HttpPost("StoreandUserData")]
        public async Task<IActionResult> StoreandUserData([FromBody] StoreUserRequest request)
        {
            
            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new ApiResponse { Success = false, Message = "Status is required." });

            try
            {
                var response = await _huDiscrepancyService.StoreandUserData(request);
                return Ok(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching StoreandUserData.");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Database error: " + ex.Message });
            }
        }
    }
}
