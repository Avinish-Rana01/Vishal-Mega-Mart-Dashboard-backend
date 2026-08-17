using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VS_Mart_Backend.Services;
using Microsoft.AspNetCore.Http;
using System;

namespace VS_Mart_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly SseNotifierService _notifier;

        public EventsController(SseNotifierService notifier)
        {
            _notifier = notifier;
        }

        [HttpGet("dashboard-refresh")]
        public async Task DashboardRefresh()
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var clientId = _notifier.RegisterClient();

            try
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    // Wait for the next refresh event or for the client to disconnect
                    await _notifier.WaitForEventAsync(clientId, HttpContext.RequestAborted);

                    if (!HttpContext.RequestAborted.IsCancellationRequested)
                    {
                        // Send the SSE message
                        await Response.WriteAsync("data: refresh\n\n");
                        await Response.Body.FlushAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                _notifier.UnregisterClient(clientId);
            }
        }
    }
}
