using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Dashboard.Hubs;

namespace VS_Mart_Backend.Features.Dashboard.Hubs
{
    [ApiController]
    [Route("api/stock")]
    public class LiveStockSimulationController : ControllerBase
    {
        private readonly IHubContext<LiveStockHub> _hubContext;

        public LiveStockSimulationController(IHubContext<LiveStockHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public class SimulatePatchRequest
        {
            public string StoreCode { get; set; } = "HD44";
            public string StoreName { get; set; } = "HD44 - Uttam Nagar 2";
            public int DeltaRfid { get; set; } = 5;
            public int CurrentRfid { get; set; } = 74539;
            public int CurrentSap { get; set; } = 92967;
            public int CurrentDiff { get; set; } = 18428;
            public decimal CurrentPercentage { get; set; } = 80.18m;
        }

        [HttpPost("simulate-live-patch")]
        public async Task<IActionResult> SimulateLivePatch([FromBody] SimulatePatchRequest? req)
        {
            req ??= new SimulatePatchRequest();

            var patch = new LiveStockDeltaPatch
            {
                Type = "STOCK_DELTA",
                Timestamp = DateTime.UtcNow,
                StoreCode = req.StoreCode,
                StoreName = req.StoreName,
                DeltaRfid = req.DeltaRfid,
                DeltaDiff = -req.DeltaRfid,
                NewRfidStock = req.CurrentRfid + req.DeltaRfid,
                NewSapStock = req.CurrentSap,
                NewDifference = req.CurrentDiff - req.DeltaRfid,
                NewPercentage = req.CurrentPercentage,
                SummaryDelta = new LiveStockSummaryDelta
                {
                    TotalRfidDelta = req.DeltaRfid,
                    TotalDiffDelta = -req.DeltaRfid,
                    NewTotalRfid = 213106 + req.DeltaRfid,
                    NewTotalDiff = 24458 - req.DeltaRfid
                }
            };

            await _hubContext.Clients.All.SendAsync("ReceiveLiveStockPatch", patch);

            return Ok(new
            {
                message = "Live patch simulated and broadcasted successfully.",
                patch
            });
        }

        [HttpGet("poller-status")]
        public IActionResult GetPollerStatus()
        {
            return Ok(VS_Mart_Backend.Services.LiveStockPollerService.GetPollerTelemetry());
        }
    }
}
