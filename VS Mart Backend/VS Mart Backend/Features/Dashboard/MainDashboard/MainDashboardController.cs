using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.MainDashboard
{
    [ApiController]
    [Route("api/Stock")]
    public class MainDashboardController : ControllerBase
    {
        private readonly IMainDashboardService _dashboardService;

        public MainDashboardController(IMainDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("live-details")]
        public async Task<IActionResult> GetLiveStockDetails([FromQuery] LiveStockQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetLiveStockDetailsAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching live stock details.", error = ex.Message });
            }
        }

        [HttpGet("tag-cycle-count")]
        public async Task<IActionResult> GetTagCycleCount([FromQuery] TagCycleCountQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetTagCycleCountDataAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching tag cycle count data.", error = ex.Message });
            }
        }

        [HttpGet("store-dashboard")]
        public async Task<IActionResult> GetStoreDashboard([FromQuery] StoreDashboardQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetStoreDashboardAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store dashboard data.", error = ex.Message });
            }
        }

        [HttpGet("sale-dashboard")]
        public async Task<IActionResult> GetSaleDashboard([FromQuery] SaleDashboardQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetSaleDashboardAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sale dashboard data.", error = ex.Message });
            }
        }

        [HttpGet("return-dashboard")]
        public async Task<IActionResult> GetReturnDashboard([FromQuery] ReturnDashboardQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetReturnDashboardAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching return dashboard data.", error = ex.Message });
            }
        }



        [HttpGet("dc-validate-dashboard")]
        public async Task<IActionResult> GetDcValidateDashboard([FromQuery] DcValidateDashboardQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetDcValidateDashboardAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching DC validate dashboard data.", error = ex.Message });
            }
        }

        [HttpGet("cycle-count-dashboard")]
        public async Task<IActionResult> GetCycleCountDashboard([FromQuery] CycleCountDashboardQueryRequest query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetCycleCountDashboardAsync(query, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count dashboard data.", error = ex.Message });
            }
        }

        [HttpGet("vendor-hu-discrepancy")]
        public async Task<IActionResult> GetVendorHUDiscrepancy([FromQuery] VendorHUDiscrepancyQueryRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            try
            {
                var result = await _dashboardService.GetVendorHUDiscrepancyAsync(request, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("tag-management-location")]
        public async Task<IActionResult> GetTagManagementLocation([FromQuery] TagManagementQueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetTagManagementDataAsync(request, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("warehouse-encoding")]
        public async Task<IActionResult> GetWarehouseEncoding([FromQuery] WarehouseEncodingQueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dashboardService.GetWarehouseEncodingDataAsync(request, cancellationToken: cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
