using VS_Mart_Backend.Features.Base;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.SaleDashboard
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaleDashboardController : ControllerBase
    {
        private readonly ISaleDashboardService _saleDashboardService;

        public SaleDashboardController(ISaleDashboardService saleDashboardService)
        {
            _saleDashboardService = saleDashboardService;
        }

        /// <summary>
        /// Retrieves store sale report metrics and dynamic grid details.
        /// </summary>
        [HttpGet("/api/Stock/store-sale-report")]
        public async Task<IActionResult> GetStoreSaleReport([FromQuery] StoreSaleReportQueryRequest query)
        {
            try
            {
                int resolvedUid = HttpContext.GetResolvedUserId(query.UserId);
                if (resolvedUid > 0) query.UserId = resolvedUid.ToString();
                var result = await _saleDashboardService.GetStoreSaleReportAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store sale report data.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/sale/pos-counters")]
        public async Task<IActionResult> BindPOSCounter([FromQuery] BindPOSCounterRequest request)
        {
            try
            {
                int resolvedUid = HttpContext.GetResolvedUserId(request.UserId);
                if (resolvedUid > 0) request.UserId = resolvedUid.ToString();
                var result = await _saleDashboardService.BindPOSCounterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching POS counters.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/sale/articles")]
        public async Task<IActionResult> SearchArticlesSale([FromQuery] SearchArticlesSaleRequest request)
        {
            try
            {
                int resolvedUid = HttpContext.GetResolvedUserId(request.UserId);
                if (resolvedUid > 0) request.UserId = resolvedUid.ToString();
                var result = await _saleDashboardService.SearchArticlesSaleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching articles.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/sale/eans")]
        public async Task<IActionResult> SearchEANSale([FromQuery] SearchEANSaleRequest request)
        {
            try
            {
                int resolvedUid = HttpContext.GetResolvedUserId(request.UserId);
                if (resolvedUid > 0) request.UserId = resolvedUid.ToString();
                var result = await _saleDashboardService.SearchEANSaleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching EANs.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/sale-data")]
        public async Task<IActionResult> GetSaleData([FromQuery] SaleDataQueryRequest request)
        {
            try
            {
                int resolvedUid = HttpContext.GetResolvedUserId(request.UserId);
                if (resolvedUid > 0) request.UserId = resolvedUid.ToString();
                var result = await _saleDashboardService.GetSaleDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sale data.", error = ex.Message });
            }
        }
    }
}
