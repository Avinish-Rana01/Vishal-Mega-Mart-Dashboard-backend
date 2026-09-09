using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Registration.Common;

namespace VS_Mart_Backend.Features.Registration.WarehouseRegistration
{
    [ApiController]
    public class WarehouseRegistrationController : ControllerBase
    {
        private readonly IWarehouseRegistrationService _warehouseService;
        private readonly ILogger<WarehouseRegistrationController> _logger;

        public WarehouseRegistrationController(IWarehouseRegistrationService warehouseService, ILogger<WarehouseRegistrationController> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }

        [HttpGet("/api/Registration/Warehouse")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all warehouses.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching warehouses."));
            }
        }

        [HttpGet("/api/Registration/Warehouse/dropdown")]
        public async Task<IActionResult> GetWarehouseDropdown([FromQuery] int userId = 0, [FromQuery] string userType = "Super Admin")
        {
            try
            {
                var dropdown = await _warehouseService.GetWarehouseDropdownAsync(userId, userType);
                return Ok(dropdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse dropdown.");
                return StatusCode(500, ActionResponse.Fail("Internal server error fetching warehouse dropdown."));
            }
        }

        [HttpPost("/api/Registration/Warehouse")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _warehouseService.CreateWarehouseAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating warehouse.");
                return StatusCode(500, ActionResponse.Fail("Internal server error creating warehouse."));
            }
        }

        [HttpPut("/api/Registration/Warehouse")]
        public async Task<IActionResult> UpdateWarehouse([FromBody] UpdateWarehouseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _warehouseService.UpdateWarehouseAsync(request);
                if (!response.Success)
                {
                    return Conflict(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating warehouse.");
                return StatusCode(500, ActionResponse.Fail("Internal server error updating warehouse."));
            }
        }

        [HttpPatch("/api/Registration/Warehouse/{whId:int}/status")]
        public async Task<IActionResult> ToggleWarehouseStatus(int whId)
        {
            if (whId <= 0)
            {
                return BadRequest(ActionResponse.Fail("Invalid warehouse ID."));
            }

            try
            {
                var response = await _warehouseService.ToggleWarehouseStatusAsync(whId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling warehouse status for warehouse {WhId}.", whId);
                return StatusCode(500, ActionResponse.Fail("Internal server error toggling warehouse status."));
            }
        }
    }
}
