using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Dashboard.Hubs
{
    public class LiveStockHub : Hub
    {
        // Broadcasts a delta patch to all connected clients
        public async Task BroadcastPatch(LiveStockDeltaPatch patch)
        {
            await Clients.All.SendAsync("ReceiveLiveStockPatch", patch);
        }
    }
}
