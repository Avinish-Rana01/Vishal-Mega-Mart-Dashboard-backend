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

        [HttpPost("/api/Auth/login")]
        public IActionResult Login(LoginRequest request)
        {
            try
            {
                var response = _systemUtilityService.Login(request);
                if (response.Success == false)
                {
                    return Unauthorized(response);
                }
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores.");
                return StatusCode(500, "An error occurred while loading stores.");
            }
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
    }
}
