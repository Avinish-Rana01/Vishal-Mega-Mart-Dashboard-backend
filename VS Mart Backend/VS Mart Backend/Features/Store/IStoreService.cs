using System.Collections.Generic;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Store
{
    /// <summary>
    /// Service contract defining operations for retrieving POS counter statuses and store-level counter configurations.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Retrieves the list of accessible stores for counter status monitoring based on the user's role and assigned permissions.
        /// </summary>
        /// <param name="userId">The unique identifier of the requesting user.</param>
        /// <returns>A response containing the collection of authorized store status records.</returns>
        Task<CounterStatusResponse<CounterStatusStoreDto>> GetCounterStatusStoresAsync(int userId);

        /// <summary>
        /// Retrieves detailed real-time counter status and metrics for a specific store location.
        /// </summary>
        /// <param name="storeId">The unique identifier of the store to inspect.</param>
        /// <param name="userId">The unique identifier of the user making the request.</param>
        /// <returns>A response containing POS counter active/inactive statuses and card metrics.</returns>
        Task<CounterStatusResponse<CounterStatusDetailDto>> GetCounterStatusDetailsAsync(int storeId, int userId = 0);
    }
}
