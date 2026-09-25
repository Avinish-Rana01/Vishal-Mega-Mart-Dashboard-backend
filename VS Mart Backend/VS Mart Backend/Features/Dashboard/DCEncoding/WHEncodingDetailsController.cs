using Microsoft.AspNetCore.Mvc;

namespace VS_Mart_Backend.Features.Dashboard.DCEncoding
{
    [ApiController]
    public class WHEncodingDetailsController : ControllerBase
    {
        private readonly IWHEncodingDetails _whEncodingDetails;

        public WHEncodingDetailsController(IWHEncodingDetails whEncodingDetails)
        {
            _whEncodingDetails = whEncodingDetails;
        }

        [HttpGet("GetWHEncodingDetails")]
        public async Task<IActionResult> GetWHEncodingDetails([FromQuery] WHEncodingRequest request)
        {
            try
            {
                var result = await _whEncodingDetails.GetWHEncodingDetailsAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = ex.Message
                    });
            }
        }

        [HttpGet("SearchUsername")]
        public async Task<IActionResult> SearchUsername(
        [FromQuery] UsernameRequest request)
        {
            var result =
                await _whEncodingDetails.SearchUsernameAsync(request);

            return Ok(result);
        }
    }
}
