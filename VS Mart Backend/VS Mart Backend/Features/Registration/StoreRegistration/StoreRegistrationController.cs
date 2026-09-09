using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.StoreRegistration
{
    [ApiController]
    public class StoreRegistrationController : ControllerBase
    {
        private readonly IStoreRegistrationService _storeService;
        private readonly ILogger<StoreRegistrationController> _logger;

        public StoreRegistrationController(IStoreRegistrationService storeService, ILogger<StoreRegistrationController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        [HttpGet("/api/Registration/Store")]
        public async Task<IActionResult> GetAllStores()
        {
            try
            {
                var stores = await _storeService.GetAllStoresAsync();
                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stores.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching stores."));
            }
        }

        [HttpGet("/api/Registration/Store/dropdown")]
        public async Task<IActionResult> GetStoreDropdown([FromQuery] int userId = 0, [FromQuery] string userType = "Super Admin")
        {
            try
            {
                var dropdown = await _storeService.GetStoreDropdownAsync(userId, userType);
                return Ok(dropdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store dropdown.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching store dropdown."));
            }
        }

        [HttpPost("/api/Registration/Store")]
        public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _storeService.CreateStoreAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store.");
                return StatusCode(500, ActionResponse.Fail("Internal server error creating store."));
            }
        }

        [HttpPut("/api/Registration/Store")]
        public async Task<IActionResult> UpdateStore([FromBody] UpdateStoreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _storeService.UpdateStoreAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store.");
                return StatusCode(500, ActionResponse.Fail("Internal server error updating store."));
            }
        }

        [HttpPatch("/api/Registration/Store/{storeId:int}/status")]
        public async Task<IActionResult> ToggleStoreStatus(int storeId)
        {
            if (storeId <= 0)
            {
                return BadRequest(ActionResponse.Fail("Invalid store ID."));
            }

            try
            {
                var response = await _storeService.ToggleStoreStatusAsync(storeId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling store status for store {StoreId}.", storeId);
                return StatusCode(500, ActionResponse.Fail("Internal server error toggling store status."));
            }
        }
    }
}
