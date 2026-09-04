using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.LiveStockReport
{
    [ApiController]
    [Route("api/report")]
    public class LiveStockReportController : ControllerBase
    {
        private readonly ILiveStockReportService _liveStockReportService;
        private readonly ILogger<LiveStockReportController> _logger;

        public LiveStockReportController(ILiveStockReportService liveStockReportService, ILogger<LiveStockReportController> logger)
        {
            _liveStockReportService = liveStockReportService;
            _logger = logger;
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetStores([FromQuery] string? userId)
        {
            try
            {
                var stores = await _liveStockReportService.GetStoresAsync(userId);
                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores.");
                return StatusCode(500, "An error occurred while loading stores."); 
            }
        }

        [HttpGet("articles/search")]
        public async Task<IActionResult> SearchArticles([FromQuery] ArticleSearchRequest request)
        {
            try
            {
                var articles = await _liveStockReportService.SearchArticlesAsync(request);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching articles.");
                return StatusCode(500, "An error occurred while searching for articles.");
            }
        }

        [HttpGet("live-stock")]
        public async Task<IActionResult> GetLiveStockDetails([FromQuery] LiveStockReportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StoreName) || 
                string.IsNullOrWhiteSpace(request.StockDate))
            {
                return BadRequest("Store Name and Stock Date are required."); 
            }

            try
            {
                var response = await _liveStockReportService.GetLiveStockDetailsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching live stock details.");
                return StatusCode(500, "An error occurred while loading the report data.");
            }
        }
    }
}
