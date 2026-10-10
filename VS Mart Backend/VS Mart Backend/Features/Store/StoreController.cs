using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Store
{
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IStoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        [HttpGet("/api/Store/counter-status-stores")]
        public async Task<IActionResult> GetCounterStatusStores([FromQuery] int userId)
        {
            try
            {
                var response = await _storeService.GetCounterStatusStoresAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching counter status stores for userId {UserId}.", userId);
                return StatusCode(500, new CounterStatusResponse<CounterStatusStoreDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching stores for counter status."
                });
            }
        }

        [HttpGet("/api/Store/counter-status")]
        public async Task<IActionResult> GetCounterStatus([FromQuery] int storeId, [FromQuery] int userId = 0)
        {
            if (storeId <= 0)
            {
                return BadRequest(new CounterStatusResponse<CounterStatusDetailDto>
                {
                    Success = false,
                    Message = "A valid positive storeId parameter is required."
                });
            }

            try
            {
                var response = await _storeService.GetCounterStatusDetailsAsync(storeId, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching counter status details for storeId {StoreId}.", storeId);
                return StatusCode(500, new CounterStatusResponse<CounterStatusDetailDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching counter status details."
                });
            }
        }
    }
}
