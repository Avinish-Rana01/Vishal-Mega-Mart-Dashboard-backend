using Microsoft.AspNetCore.Mvc;
using VS_Mart_Backend.Models;
using VS_Mart_Backend.Services;

namespace VS_Mart_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly ILiveStockService _liveStockService;

        public StockController(ILiveStockService liveStockService)
        {
            _liveStockService = liveStockService;
        }

        /// <summary>
        /// Retrieves live stock dashboard summary and store-wise table rows.
        /// </summary>
        [HttpGet("live-details")]
        public async Task<IActionResult> GetLiveStockDetails([FromQuery] LiveStockQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetLiveStockDetailsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching live stock details.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves detailed article-level live stock report records.
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetLiveStockReport([FromQuery] LiveStockReportQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetLiveStockReportAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching live stock report.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves tag cycle count data and summary metrics.
        /// </summary>
        [HttpGet("tag-cycle-count")]
        public async Task<IActionResult> GetTagCycleCount([FromQuery] TagCycleCountQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetTagCycleCountDataAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching tag cycle count data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves cycle count report view data (dashboard summary of cycles).
        /// </summary>
        [HttpGet("cycle-count-report")]
        public async Task<IActionResult> GetCycleCountReportView([FromQuery] CycleCountReportViewQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetCycleCountReportViewAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count report.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves cycle count dashboard data.
        /// </summary>
        [HttpGet("cycle-count-dashboard")]
        public async Task<IActionResult> GetCycleCountDashboard([FromQuery] CycleCountDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetCycleCountDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count dashboard data.", error = ex.Message });
            }
        }
    }
}
