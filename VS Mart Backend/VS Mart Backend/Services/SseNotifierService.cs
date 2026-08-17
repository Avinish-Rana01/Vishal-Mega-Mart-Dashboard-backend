using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Services
{
    public class SseNotifierService
    {
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _clients = new();

        public Guid RegisterClient()
        {
            var id = Guid.NewGuid();
            _clients.TryAdd(id, new SemaphoreSlim(0));
            return id;
        }

        public void UnregisterClient(Guid id)
        {
            if (_clients.TryRemove(id, out var semaphore))
            {
                semaphore.Dispose();
            }
        }

        public async Task WaitForEventAsync(Guid id, CancellationToken ct)
        {
            if (_clients.TryGetValue(id, out var semaphore))
            {
                await semaphore.WaitAsync(ct);
            }
        }

        public void NotifyRefresh()
        {
            foreach (var kvp in _clients)
            {
                // Release the semaphore if it's currently blocking a request
                if (kvp.Value.CurrentCount == 0)
                {
                    try
                    {
                        kvp.Value.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore if already disposed
                    }
                }
            }
        }
    }
}
