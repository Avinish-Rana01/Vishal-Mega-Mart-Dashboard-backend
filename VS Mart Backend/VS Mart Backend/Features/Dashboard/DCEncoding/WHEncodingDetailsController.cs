using Microsoft.AspNetCore.Mvc;

namespace VS_Mart_Backend.Features.Dashboard.DCEncoding
{
    [ApiController]
    [Route("api/dc-encoding")]
    [Route("api/[controller]")]
    public class WHEncodingDetailsController : ControllerBase
    {
        private readonly IWHEncodingDetails _whEncodingDetails;
        private readonly ILogger<WHEncodingDetailsController> _logger;

        public WHEncodingDetailsController(IWHEncodingDetails whEncodingDetails, ILogger<WHEncodingDetailsController> logger)
        {
            _whEncodingDetails = whEncodingDetails;
            _logger = logger;
        }

        [HttpGet("details")]
        [HttpGet("GetWHEncodingDetails")]
        [HttpGet("/GetWHEncodingDetails")]
        public async Task<IActionResult> GetWHEncodingDetails([FromQuery] WHEncodingRequest request)
        {
            try
            {
                var result = await _whEncodingDetails.GetWHEncodingDetailsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DC WH encoding details.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "An error occurred while fetching warehouse encoding details."
                    });
            }
        }

        [HttpGet("users")]
        [HttpGet("SearchUsername")]
        [HttpGet("/SearchUsername")]
        public async Task<IActionResult> SearchUsername([FromQuery] UsernameRequest request)
        {
            try
            {
                var result = await _whEncodingDetails.SearchUsernameAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching username in DC WH encoding.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "An error occurred while searching users."
                    });
            }
        }
    }
}
