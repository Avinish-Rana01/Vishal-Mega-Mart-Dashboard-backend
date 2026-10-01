using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Auth
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("/api/Auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authService.LoginAsync(request, cancellationToken);
                if (response.Success == false)
                {
                    return Unauthorized(response);
                }
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Client Closed Request");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication.");
                return StatusCode(500, "An error occurred during authentication.");
            }
        }

        [HttpPost("/api/Auth/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authService.ChangePasswordAsync(request, cancellationToken);
                if (!response.Success)
                {
                    return BadRequest(response);
                }
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, "Client Closed Request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change.");
                return StatusCode(500, new ChangePasswordResponse
                {
                    Success = false,
                    Message = "An error occurred while changing password."
                });
            }
        }
    }
}
