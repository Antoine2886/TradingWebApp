using Bd.Enums;
using Bd.Infrastructure;
using WebApp.Utilities.Trading.DTO;

namespace WebApp.Utilities.Trading
{
    public interface ITradeService
    {
        Task ConnectWebSocket();
        Task UpdateStocks(string name, string interval, DateTime? startDate = null);
        Task<StockDataDto> GetLatestData(string symbol, string interval);
        Task<BidAskDto> GetBidAskData(string symbol);
        Task<BalanceDto> GetBalanceData(Guid userId);
        Task<List<OrderDto>> GetOrderData(Guid userId);
        Task ExecuteOrder(Order order, Guid userId);
        Task PlaceOrder(Guid userId, string symbol, OrderType orderType, float quantityInLots, float price, float? stopLoss = null, float? takeProfit = null, OrderState orderState = OrderState.Opened);
        Task OpenPendingOrder(Guid orderId, Guid userId);
        Task<float> GetBidPriceForSymbol(string symbol);

    }
}
