using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public SystemUtilityController(
            ISystemUtilityService systemUtilityService, 
            ILogger<SystemUtilityController> logger,
            IConfiguration configuration)
        {
            _systemUtilityService = systemUtilityService;
            _logger = logger;
            _configuration = configuration;
        }


        [HttpGet("/api/Stock/cache-status")]
        public IActionResult GetCacheStatus()
        {
            return Ok(new { cacheEnabled = _systemUtilityService.IsCacheEnabled() });
        }

        [HttpGet("/api/Stock/toggle-cache")]
        [HttpPost("/api/Stock/toggle-cache")]
        public IActionResult ToggleCache(
            [FromQuery] bool enabled, 
            [FromQuery] string? secret, 
            [FromHeader(Name = "X-Admin-Secret")] string? headerSecret)
        {
            string configuredSecret = _configuration.GetValue<string>("DashboardSettings:AdminSecretKey") ?? "VMM-Admin-Secret-2026";
            string providedSecret = (!string.IsNullOrWhiteSpace(secret) ? secret : headerSecret) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(providedSecret) || !string.Equals(providedSecret.Trim(), configuredSecret.Trim(), StringComparison.Ordinal))
            {
                _logger.LogWarning("Unauthorized cache toggle attempt from IP: {Ip}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new 
                { 
                    success = false, 
                    error = "Unauthorized. A valid secret key is required via '?secret=...' parameter or 'X-Admin-Secret' header." 
                });
            }

            _systemUtilityService.SetCacheEnabled(enabled);
            if (enabled)
            {
                VS_Mart_Backend.Services.CacheWarmerService.TriggerImmediateWarmup();
            }
            _logger.LogInformation("Cache system toggled to {Status} by authorized admin.", enabled ? "ENABLED" : "DISABLED");

            return Ok(new 
            { 
                success = true,
                message = $"Cache system is now {(enabled ? "ENABLED" : "DISABLED")}.", 
                cacheEnabled = enabled,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
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
