using Microsoft.AspNetCore.Mvc;
using VS_Mart_Backend.Features.StoreGrcReport;

namespace VS_Mart_Backend.Features.Dashboard.TagManagement
{
    [ApiController]
    [Route("api/Stock")]
    [Route("api/[controller]")]
    public class TagManagementController : ControllerBase
    {
        private readonly ITagManagement _service;
        private readonly ILogger<TagManagementController> _logger;

        public TagManagementController(ITagManagement service, ILogger<TagManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("GetTagDetails")]
        [HttpGet("/GetTagDetails")]
        public async Task<IActionResult> GetTagDetails([FromQuery] TagDetailsRequest request)
        {
            try
            {
                var result = await _service.GetTagDetailsAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message
                    });
            }
        }

    }
}
