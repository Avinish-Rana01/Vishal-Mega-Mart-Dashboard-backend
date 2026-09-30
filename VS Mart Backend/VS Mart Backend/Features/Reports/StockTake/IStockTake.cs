namespace VS_Mart_Backend.Features.Reports.StockTake
{
    public interface IStockTake
    {
        Task<StockTakeResponse> GetStockTakeDataAsync(StockTakeRequest request,int userId);
    }
}
