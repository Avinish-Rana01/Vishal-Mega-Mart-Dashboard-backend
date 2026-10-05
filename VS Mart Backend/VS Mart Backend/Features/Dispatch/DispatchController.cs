using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dispatch
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispatchController : ControllerBase
    {
        private readonly IDispatchService _dispatchService;

        public DispatchController(IDispatchService dispatchService)
        {
            _dispatchService = dispatchService;
        }

        /// <summary>
        /// Master Upload API for RDC Master and HU Input (and Picklist if passed with status='PICKLIST')
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
        public async Task<IActionResult> UploadMaster([FromForm] IFormFile file, [FromForm] string status, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file was uploaded." });
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new { success = false, message = "Upload status is required ('RDC_MASTER' or 'HU_INPUT')." });
            }

            var result = await _dispatchService.ProcessUploadAsync(file, status, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Picklist Excel Upload API
        /// </summary>
        [HttpPost("picklist-upload")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicklist([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file was uploaded." });
            }

            var result = await _dispatchService.ProcessPicklistUploadAsync(file, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Dispatch Tracking - View Report API (SP_NEW_REPORT status='DISPATCH_REPORT_DATA')
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetReport([FromQuery] DispatchReportRequest request, CancellationToken cancellationToken)
        {
            var result = await _dispatchService.GetDispatchReportAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Dispatch Tracking - Vehicle Modal Drill-Down API (SP_NEW_REPORT status='DISPATCH_REPORT_DATA_VIEW')
        /// </summary>
        [HttpGet("report-details")]
        public async Task<IActionResult> GetReportDetails([FromQuery] DispatchReportModalRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.VehicleNo))
            {
                return BadRequest(new { success = false, message = "Vehicle No is required for details modal." });
            }

            var result = await _dispatchService.GetDispatchReportModalAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
