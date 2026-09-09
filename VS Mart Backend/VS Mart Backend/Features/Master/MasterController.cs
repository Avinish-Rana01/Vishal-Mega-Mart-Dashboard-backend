using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Master
{
    [ApiController]
    public class MasterController : ControllerBase
    {
        private readonly IMasterService _masterService;
        private readonly ILogger<MasterController> _logger;

        public MasterController(IMasterService masterService, ILogger<MasterController> logger)
        {
            _masterService = masterService;
            _logger = logger;
        }

        [HttpPost("/api/Master/Execute")]
        public async Task<IActionResult> Execute([FromBody] MasterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new MasterResponse
                {
                    Success = false,
                    Message = "A valid Status string is required."
                });
            }

            try
            {
                var response = await _masterService.ExecuteMasterAsync(request);
                if (!response.Success)
                {
                    return StatusCode(400, response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing universal Master execution for status {Status}.", request.Status);
                return StatusCode(500, new MasterResponse
                {
                    Success = false,
                    Message = "An internal server error occurred while executing the master operation."
                });
            }
        }
    }
}
