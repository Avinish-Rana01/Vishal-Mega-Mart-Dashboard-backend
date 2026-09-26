using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/reports")]
    public class UniversalExportController : ControllerBase
    {
        private readonly IUniversalExportService _exportService;
        private readonly ILogger<UniversalExportController> _logger;

        public UniversalExportController(IUniversalExportService exportService, ILogger<UniversalExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        [HttpGet("export")]
        public async Task ExportReport([FromQuery] UniversalExportRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ReportName))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("Query parameter 'reportName' is required.", cancellationToken);
                return;
            }

            if (!ReportRegistry.Contains(request.ReportName))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                await Response.WriteAsync($"Report '{request.ReportName}' is not recognized.", cancellationToken);
                return;
            }

            bool isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);
            string extension = isCsv ? "csv" : "xlsx";
            string contentType = isCsv
                ? "text/csv; charset=utf-8"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            string safeFileName = $"{request.ReportName.Trim()}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

            try
            {
                using var memoryBuffer = new System.IO.MemoryStream();
                bool hasData = await _exportService.StreamExportAsync(request, memoryBuffer, cancellationToken);

                if (!hasData || memoryBuffer.Length == 0)
                {
                    _logger.LogInformation("Export requested for {ReportName} produced 0 rows. Returning 204 No Content.", request.ReportName);
                    Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                Response.ContentType = contentType;
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{safeFileName}\"");
                Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                Response.Headers.Append("Pragma", "no-cache");
                Response.Headers.Append("Expires", "0");
                Response.Headers.Append("Content-Length", memoryBuffer.Length.ToString());

                memoryBuffer.Position = 0;
                await memoryBuffer.CopyToAsync(Response.Body, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Export stream for {ReportName} was cancelled by client.", request.ReportName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed streaming report export for {ReportName}", request.ReportName);
                if (!Response.HasStarted)
                {
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await Response.WriteAsync($"Export Error: {ex.Message}", cancellationToken);
                }
            }
        }
    }
}
