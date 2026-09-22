using Microsoft.AspNetCore.Mvc;

namespace VS_Mart_Backend.Features.Reports.StockTake
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockTakeController : ControllerBase
    {

        private readonly IStockTake _stockTake;
        private readonly ILogger<StockTakeController> _logger;

        public StockTakeController(IStockTake stockTake, ILogger<StockTakeController> logger)
        {
            _stockTake = stockTake;
            _logger = logger;
        }

        [HttpGet("GetStockTakeData")]
        public async Task<IActionResult> GetStockTakeData([FromQuery] StockTakeRequest request)
        {
            try
            {
                // Later get this from JWT claims
                int userId = 30;

                var result = await _stockTake.GetStockTakeDataAsync(request, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = ex.Message
                    });
            }
        }

    }
}
