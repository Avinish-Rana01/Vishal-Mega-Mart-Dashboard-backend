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

            Response.ContentType = contentType;
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{safeFileName}\"");
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            try
            {
                await _exportService.StreamExportAsync(request, Response.Body, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Export stream for {ReportName} was cancelled by client.", request.ReportName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed streaming report export for {ReportName}", request.ReportName);
                try
                {
                    await using var errorWriter = new System.IO.StreamWriter(Response.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                    await errorWriter.WriteLineAsync($"\n--- EXPORT ERROR ---\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    await errorWriter.FlushAsync();
                }
                catch
                {
                    // Ignore secondary stream errors
                }
            }
        }
    }
}
