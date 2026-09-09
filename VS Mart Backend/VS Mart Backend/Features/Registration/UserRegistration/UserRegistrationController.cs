using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.UserRegistration
{
    [ApiController]
    public class UserRegistrationController : ControllerBase
    {
        private readonly IUserRegistrationService _userService;
        private readonly ILogger<UserRegistrationController> _logger;

        public UserRegistrationController(IUserRegistrationService userService, ILogger<UserRegistrationController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("/api/Registration/User")]
        public async Task<IActionResult> GetUsers([FromQuery] int userId = 0, [FromQuery] string userType = "Super Admin")
        {
            try
            {
                var users = await _userService.GetUsersAsync(userId, userType);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching users."));
            }
        }

        [HttpGet("/api/Registration/User/roles")]
        public async Task<IActionResult> GetUserRoles()
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching user roles."));
            }
        }

        [HttpPost("/api/Registration/User")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _userService.CreateUserAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user.");
                return StatusCode(500, ActionResponse.Fail("Internal server error creating user."));
            }
        }

        [HttpPut("/api/Registration/User")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _userService.UpdateUserAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user.");
                return StatusCode(500, ActionResponse.Fail("Internal server error updating user."));
            }
        }

        [HttpPatch("/api/Registration/User/{userId:int}/status")]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(ActionResponse.Fail("Invalid user ID."));
            }

            try
            {
                var response = await _userService.ToggleUserStatusAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for user {UserId}.", userId);
                return StatusCode(500, ActionResponse.Fail("Internal server error toggling user status."));
            }
        }
    }
}
