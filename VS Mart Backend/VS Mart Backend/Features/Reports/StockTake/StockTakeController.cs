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
                int userId = 0;
                if (!string.IsNullOrWhiteSpace(request?.UserId) && int.TryParse(request.UserId, out int parsedId))
                {
                    userId = parsedId;
                }
                else if (HttpContext.Items.TryGetValue("UserId", out var ctxUserId) && ctxUserId is int idVal)
                {
                    userId = idVal;
                }

                var result = await _stockTake.GetStockTakeDataAsync(request ?? new StockTakeRequest(), userId);

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
