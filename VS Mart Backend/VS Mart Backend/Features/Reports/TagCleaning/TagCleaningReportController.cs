
using Microsoft.AspNetCore.Mvc;
using System.Data;
using VS_Mart_Backend.Features.Master;

namespace VS_Mart_Backend.Features.Reports
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagCleaningReportController : ControllerBase
    {

        private readonly ITagCleaningReport _tagCleaningService;
        private readonly ILogger<TagCleaningReportController> _logger;

        public TagCleaningReportController(ITagCleaningReport tagCleaningService, ILogger<TagCleaningReportController> logger)
        {
            _tagCleaningService = tagCleaningService;
            _logger = logger;
        }



        [HttpPost("GetTagCleaningReport")]
        public async Task<IActionResult> GetTagCleaningReport([FromBody] TagCleaningReportRequest request)
        {
            try
            {
                var result = await _tagCleaningService.GetTagCleaningReportAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing universal GetTagCleaningReport execution for status {Status}.");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An internal server error occurred while executing the GetTagCleaningReport operation."
                });
            }
            
            

        }
    }
}
