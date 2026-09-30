using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Store
{
    public interface IStoreService
    {
        Task<CounterStatusResponse<CounterStatusStoreDto>> GetCounterStatusStoresAsync(int userId);
        Task<CounterStatusResponse<CounterStatusDetailDto>> GetCounterStatusDetailsAsync(int storeId);
    }
}
