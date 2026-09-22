using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.SystemUtility
{
    [ApiController]
    public class SystemUtilityController : ControllerBase
    {
        private readonly ISystemUtilityService _systemUtilityService;
        private readonly ILogger<SystemUtilityController> _logger;

        public SystemUtilityController(ISystemUtilityService systemUtilityService, ILogger<SystemUtilityController> logger)
        {
            _systemUtilityService = systemUtilityService;
            _logger = logger;
        }


        [HttpGet("/api/Stock/cache-status")]
        public IActionResult GetCacheStatus()
        {
            return Ok(new { cacheEnabled = _systemUtilityService.IsCacheEnabled() });
        }

        [HttpPost("/api/Stock/toggle-cache")]
        public IActionResult ToggleCache([FromQuery] bool enabled)
        {
            _systemUtilityService.SetCacheEnabled(enabled);
            return Ok(new { message = $"Cache system is now {(enabled ? "ENABLED" : "DISABLED")}.", cacheEnabled = enabled });
        }

        [HttpGet("/api/Stock/GetEncodingStoreData")]
        public async Task<IActionResult> GetEncodingStoreData([FromQuery] EncodingStoreDataRequest request)
        {
            try
            {
                var result = await _systemUtilityService.GetEncodingStoreDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching encoding store data.",
                    error = ex.Message
                });
            }
        }
        [HttpGet("/api/Stock/encoding-store-SearchEAN")]
        public async Task<IActionResult> SearchEAN([FromQuery] EncodingStoreSearchRequest request)
        {
            try
            {
                var result = await _systemUtilityService.GetEncodingStoreSearchEANAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching EANs.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/encoding-store-SearchArticle")]
        public async Task<IActionResult> SearchArticle([FromQuery] EncodingStoreSearchRequest request)
        {
            try
            {
                var result = await _systemUtilityService.GetEncodingStoreSearchArticleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching Articles.", error = ex.Message });
            }
        }

        [HttpGet("/api/Stock/GetEncodingReportDetailsModal")]
        [HttpGet("/api/Stock/encoding-report-details-modal")]
        public async Task<IActionResult> GetEncodingReportDetailsModal([FromQuery] EncodingStoreDataRequest request)
        {
            try
            {
                var result = await _systemUtilityService.GetEncodingReportDetailsModalAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching encoding report details modal.", error = ex.Message });
            }
        }

        [HttpPost("/api/Stock/GetEncodingReportDetailsModal")]
        public async Task<IActionResult> PostEncodingReportDetailsModal([FromBody] EncodingStoreDataRequest request)
        {
            try
            {
                var result = await _systemUtilityService.GetEncodingReportDetailsModalAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching encoding report details modal.", error = ex.Message });
            }
        }
    }
}
