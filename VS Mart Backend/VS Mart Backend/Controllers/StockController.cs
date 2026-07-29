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
        /// Retrieves the store dashboard aggregated metrics.
        /// </summary>
        [HttpGet("store-dashboard")]
        public async Task<IActionResult> GetStoreDashboard([FromQuery] StoreDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetStoreDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store dashboard data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the sale dashboard aggregated metrics.
        /// </summary>
        [HttpGet("sale-dashboard")]
        public async Task<IActionResult> GetSaleDashboard([FromQuery] SaleDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetSaleDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sale dashboard data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the return dashboard aggregated metrics.
        /// </summary>
        [HttpGet("return-dashboard")]
        public async Task<IActionResult> GetReturnDashboard([FromQuery] ReturnDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetReturnDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching return dashboard data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the void dashboard aggregated metrics.
        /// </summary>
        [HttpGet("void-dashboard")]
        public async Task<IActionResult> GetVoidDashboard([FromQuery] VoidDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetVoidDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching void dashboard data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the DC validate dashboard aggregated metrics.
        /// </summary>
        [HttpGet("dc-validate-dashboard")]
        public async Task<IActionResult> GetDcValidateDashboard([FromQuery] DcValidateDashboardQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetDcValidateDashboardAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching DC validate dashboard data.", error = ex.Message });
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

        /// <summary>
        /// Retrieves vendor HU discrepancy dashboard data.
        /// </summary>
        [HttpGet("vendor-hu-discrepancy")]
        public async Task<IActionResult> GetVendorHUDiscrepancy([FromQuery] VendorHUDiscrepancyQueryRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            try
            {
                var result = await _liveStockService.GetVendorHUDiscrepancyAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves tag management location data (Store vs Warehouse).
        /// </summary>
        [HttpGet("tag-management-location")]
        public async Task<IActionResult> GetTagManagementLocation([FromQuery] TagManagementQueryRequest request)
        {
            try
            {
                var result = await _liveStockService.GetTagManagementDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves warehouse encoding data by hour.
        /// </summary>
        [HttpGet("warehouse-encoding")]
        public async Task<IActionResult> GetWarehouseEncoding([FromQuery] WarehouseEncodingQueryRequest request)
        {
            try
            {
                var result = await _liveStockService.GetWarehouseEncodingDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves current backend cache status (Enabled or Disabled).
        /// </summary>
        [HttpGet("cache-status")]
        public IActionResult GetCacheStatus()
        {
            return Ok(new { cacheEnabled = _liveStockService.IsCacheEnabled() });
        }

        /// <summary>
        /// Dynamically enables or disables the cache system at runtime.
        /// </summary>
        [HttpPost("toggle-cache")]
        public IActionResult ToggleCache([FromQuery] bool enabled)
        {
            _liveStockService.SetCacheEnabled(enabled);
            return Ok(new { message = $"Cache system is now {(enabled ? "ENABLED" : "DISABLED")}.", cacheEnabled = enabled });
        }
    }
}
