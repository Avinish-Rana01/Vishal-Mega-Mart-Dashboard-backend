using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.CycleCountReport
{
    [ApiController]
    public class CycleCountReportController : ControllerBase
    {
        private readonly ICycleCountReportService _cycleCountReportService;

        public CycleCountReportController(ICycleCountReportService cycleCountReportService)
        {
            _cycleCountReportService = cycleCountReportService;
        }

        /// <summary>
        /// Retrieves cycle count report view data (dashboard summary of cycles).
        /// </summary>
        [HttpGet("/api/Stock/cycle-count-report")]
        public async Task<IActionResult> GetCycleCountReportView([FromQuery] CycleCountReportViewQueryRequest query)
        {
            try
            {
                var result = await _cycleCountReportService.GetCycleCountReportViewAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count report.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves detailed drill-down data for a specific cycle count (requires ref_no).
        /// </summary>
        [HttpGet("/api/Stock/cycle-count-details")]
        public async Task<IActionResult> GetCycleCountDetails([FromQuery] CycleCountDetailsQueryRequest query)
        {
            try
            {
                var result = await _cycleCountReportService.GetCycleCountDetailsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching cycle count details.", error = ex.Message });
            }
        }
    }
}
