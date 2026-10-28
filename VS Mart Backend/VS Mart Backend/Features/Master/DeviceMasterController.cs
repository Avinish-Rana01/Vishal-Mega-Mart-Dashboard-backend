using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Master
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceMasterController : ControllerBase
    {
        private const string DEVELOPER_USER_ID = "99999";
        private readonly IDeviceMasterService _deviceService;
        private readonly ILogger<DeviceMasterController> _logger;

        public DeviceMasterController(IDeviceMasterService deviceService, ILogger<DeviceMasterController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        /// <summary>
        /// Validates that the request originates from the authorized developer account (User ID: 99999).
        /// </summary>
        private bool IsAuthorizedDeveloper()
        {
            // 1. Check X-User-Id header
            if (Request.Headers.TryGetValue("X-User-Id", out var headerVal))
            {
                if (string.Equals(headerVal.ToString().Trim(), DEVELOPER_USER_ID, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 2. Check query parameter userId
            if (Request.Query.TryGetValue("userId", out var queryVal))
            {
                if (string.Equals(queryVal.ToString().Trim(), DEVELOPER_USER_ID, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        [HttpGet("List")]
        public async Task<IActionResult> GetDevices([FromQuery] int roleId = 4, [FromQuery] int userId = 99999, CancellationToken cancellationToken = default)
        {
            if (!IsAuthorizedDeveloper())
            {
                _logger.LogWarning("Unauthorized attempt to access DeviceMaster/List without developer user ID 99999.");
                return StatusCode(403, new { success = false, message = "Access Denied: Only authorized developer account (User ID 99999) can access Device Master APIs." });
            }

            try
            {
                var list = await _deviceService.GetDevicesAsync(roleId, userId, cancellationToken);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device directory.");
                return StatusCode(500, new { success = false, message = "Failed to fetch device list." });
            }
        }

        [HttpGet("Dropdowns")]
        public async Task<IActionResult> GetDropdowns(CancellationToken cancellationToken = default)
        {
            if (!IsAuthorizedDeveloper())
            {
                _logger.LogWarning("Unauthorized attempt to access DeviceMaster/Dropdowns without developer user ID 99999.");
                return StatusCode(403, new { success = false, message = "Access Denied: Only authorized developer account (User ID 99999) can access Device Master APIs." });
            }

            try
            {
                var data = await _deviceService.GetDropdownsAsync(cancellationToken);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device dropdowns.");
                return StatusCode(500, new { success = false, message = "Failed to fetch dropdown options." });
            }
        }

        [HttpPost("Save")]
        public async Task<IActionResult> SaveDevice([FromBody] SaveDeviceRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsAuthorizedDeveloper() && request?.User_ID != 99999)
            {
                _logger.LogWarning("Unauthorized attempt to save device without developer user ID 99999.");
                return StatusCode(403, new { success = false, message = "Access Denied: Only authorized developer account (User ID 99999) can save devices." });
            }

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Device_ESN) || request.Device_Type_ID <= 0)
                {
                    return BadRequest(new { success = false, message = "Device Type and ESN / MAC address are required." });
                }

                // Force Developer user id
                request.User_ID = 99999;

                var (success, message) = await _deviceService.SaveDeviceAsync(request, cancellationToken);
                if (!success)
                {
                    return BadRequest(new { success = false, message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving device: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Failed to save device: " + ex.Message });
            }
        }

        [HttpPost("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleDeviceStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsAuthorizedDeveloper())
            {
                _logger.LogWarning("Unauthorized attempt to toggle device status without developer user ID 99999.");
                return StatusCode(403, new { success = false, message = "Access Denied: Only authorized developer account (User ID 99999) can toggle device status." });
            }

            try
            {
                if (request == null || request.Device_ID <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid Device ID." });
                }

                var (success, message) = await _deviceService.ToggleStatusAsync(request.Device_ID, cancellationToken);
                if (!success)
                {
                    return BadRequest(new { success = false, message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling device status.");
                return StatusCode(500, new { success = false, message = "Failed to toggle status." });
            }
        }
    }
}
