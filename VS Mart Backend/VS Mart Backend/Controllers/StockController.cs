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
        /// Retrieves the store GRC report data with custom date ranges.
        /// </summary>
        [HttpGet("store-grc-report")]
        public async Task<IActionResult> GetStoreGrcReport([FromQuery] StoreGrcReportQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetStoreGrcReportAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store GRC report data.", error = ex.Message });
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
        /// Retrieves detailed drill-down data for a specific cycle count (requires ref_no).
        /// </summary>
        [HttpGet("cycle-count-details")]
        public async Task<IActionResult> GetCycleCountDetails([FromQuery] CycleCountDetailsQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetCycleCountDetailsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count details.", error = ex.Message });
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
        /// Retrieves store sale report metrics and dynamic grid details.
        /// </summary>
        [HttpGet("store-sale-report")]
        public async Task<IActionResult> GetStoreSaleReport([FromQuery] StoreSaleReportQueryRequest query)
        {
            try
            {
                var result = await _liveStockService.GetStoreSaleReportAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store sale report data.", error = ex.Message });
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

        // --- Sale Details Endpoints ---

        [HttpGet("sale/pos-counters")]
        public async Task<IActionResult> BindPOSCounter([FromQuery] BindPOSCounterRequest request)
        {
            try
            {
                var result = await _liveStockService.BindPOSCounterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching POS counters.", error = ex.Message });
            }
        }

        [HttpGet("sale/articles")]
        public async Task<IActionResult> SearchArticlesSale([FromQuery] SearchArticlesSaleRequest request)
        {
            try
            {
                var result = await _liveStockService.SearchArticlesSaleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching articles.", error = ex.Message });
            }
        }

        [HttpGet("sale/eans")]
        public async Task<IActionResult> SearchEANSale([FromQuery] SearchEANSaleRequest request)
        {
            try
            {
                var result = await _liveStockService.SearchEANSaleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching EANs.", error = ex.Message });
            }
        }

        [HttpGet("sale-data")]
        public async Task<IActionResult> GetSaleData([FromQuery] SaleDataQueryRequest request)
        {
            try
            {
                var result = await _liveStockService.GetSaleDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sale data.", error = ex.Message });
            }
        }

        [HttpGet("GetVoidDetails")]
        public async Task<IActionResult> GetVoidDetails([FromQuery] VoidDetailsRequest request)
        {
            try
            {
                var result = await _liveStockService.GetVoidDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Void details.", error = ex.Message });
            }
        }
        [HttpGet("GetVoidReconciliationData")]
        public async Task<IActionResult> GetVoidReconciliationData([FromQuery] VoidReconciliationRequest request)
        {
            try
            {
                var result = await _liveStockService.GetVoidReconciliationDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Void reconciliation data.", error = ex.Message });
            }
        }

        [HttpGet("void/pos-counters")]
        public async Task<IActionResult> VoidBindPOSCounter([FromQuery] BindPOSCounterRequest request)
        {
            try
            {
                var result = await _liveStockService.VoidBindPOSCounter(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching POS counters.", error = ex.Message });
            }
        }
        [HttpGet("void-SearchEAN")]
        public async Task<IActionResult> SearchEAN([FromQuery] SearchEANRequest request)
        {
            try
            {
                var result = await _liveStockService.SearchEAN(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching EANs.", error = ex.Message });
            }
        }

        [HttpGet("dashboard/return-details")]
        public async Task<IActionResult> GetReturnDetails([FromQuery] ReturnDetailsRequest request)
        {
            try
            {
                var result = await _liveStockService.GetReturnDetailsAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching return details.", error = ex.Message });
            }
        }

        [HttpGet("void/return-reconciliation")]
        public IActionResult GetReturnReconciliationData([FromQuery] ReturnReconciliationRequest request)
        {
            try
            {
                var result = _liveStockService.GetReturnReconciliationData(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching return reconciliation data.", error = ex.Message });
            }
        }

        [HttpGet("GetDCDetails")]
        public async Task<IActionResult> GetDCDetails([FromQuery] DCDetailsRequest request)
        {
            try
            {
                var result = await _liveStockService.GetDCDetails(request);

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
                var result = await _liveStockService.GetHUDetails(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching HU details.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetEncodingStoreData")]
        public async Task<IActionResult> GetEncodingStoreData([FromQuery] EncodingStoreDataRequest request)
        {
            try
            {
                var result = await _liveStockService.GetEncodingStoreData(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        "An error occurred while fetching encoding store data.",
                    error = ex.Message
                });
            }
        }

    }
}

