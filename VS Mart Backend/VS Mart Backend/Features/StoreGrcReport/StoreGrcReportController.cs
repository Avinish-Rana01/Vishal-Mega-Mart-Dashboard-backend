using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.StoreGrcReport
{
    [ApiController]
    [Route("api/grc-report")]
    public class StoreGrcReportController : ControllerBase
    {
        private readonly IStoreGrcReportService _service;
        private readonly ILogger<StoreGrcReportController> _logger;

        public StoreGrcReportController(IStoreGrcReportService service, ILogger<StoreGrcReportController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("hu-numbers/search")]
        public async Task<IActionResult> SearchHuNumbers([FromQuery] GrcHuSearchRequest request)
        {
            try
            {
                var result = await _service.SearchHuNumbersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching HU numbers.");
                return StatusCode(500, "An error occurred while loading HU numbers.");
            }
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetGrcDetails([FromQuery] GrcDetailsRequest request)
        {
            try
            {
                var result = await _service.GetGrcDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC details.");
                return StatusCode(500, "An error occurred while loading the report data.");
            }
        }

        [HttpGet("modal-details")]
        public async Task<IActionResult> GetGrcModalDetails([FromQuery] GrcModalDetailsRequest request)
        {
            try
            {
                var result = await _service.GetGrcModalDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC modal details.");
                return StatusCode(500, "An error occurred while loading the modal data.");
            }
        }
        [HttpGet("/api/stock/store-grc-report")]
        public async Task<IActionResult> GetStoreGrcReport([FromQuery] StoreGrcReportQueryRequest query)
        {
            try
            {
                var result = await _service.GetStoreGrcReportAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching store GRC report data.", error = ex.Message });
            }
        }

        [HttpGet("/api/stock/Hu-details")]
        public async Task<IActionResult> GetHUDetails([FromQuery] HUDetailsRequest request)
        {
            try
            {
                var result = await _service.GetHUDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching HU details.", error = ex.Message });
            }
        }
    }
}
