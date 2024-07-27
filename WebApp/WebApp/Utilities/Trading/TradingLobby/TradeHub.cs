using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebApp.Utilities.Trading.TradingLobby
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Hub for managing real-time trade updates, new candle notifications, balance updates, and order updates.
    /// </summary>
    public class TradeHub : Hub
    {
        private readonly ITradeService _tradeService;


        /// <summary>
        /// Initializes a new instance of the TradeHub class.
        /// </summary>
        /// <param name="tradeService">An instance of the ITradeService for handling trade-related operations.</param>
        public TradeHub(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        /// <summary>
        /// Sends a trade update to all connected clients.
        /// </summary>
        /// <param name="symbol">The symbol of the trade.</param>
        /// <param name="price">The price of the trade.</param>
        /// <param name="bid">The bid price of the trade.</param>
        /// <param name="ask">The ask price of the trade.</param>
        /// <param name="timestamp">The timestamp of the trade.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendTradeUpdate(string symbol, float price, float bid, float ask, DateTime timestamp)
        {
            var message = new
            {
                symbol,
                price,
                bid,
                ask,
                timestamp
            };

            await Clients.All.SendAsync("ReceiveTradeUpdate", Newtonsoft.Json.JsonConvert.SerializeObject(message));
        }


        /// <summary>
        /// Sends a new candle update to all connected clients.
        /// </summary>
        /// <param name="symbol">The symbol of the trade.</param>
        /// <param name="timeStamp">The timestamp of the candle.</param>
        /// <param name="open">The opening price of the candle.</param>
        /// <param name="high">The highest price of the candle.</param>
        /// <param name="low">The lowest price of the candle.</param>
        /// <param name="close">The closing price of the candle.</param>
        /// <param name="interval">The interval of the candle.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendNewCandle(string symbol, DateTime timeStamp, float open, float high, float low, float close, string interval)
        {
            var message = new
            {
                Symbol = symbol,
                TimeStamp = timeStamp,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Interval = interval
            };

            await Clients.All.SendAsync("NewCandle", Newtonsoft.Json.JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// Sends a balance update to a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendBalanceUpdate(Guid userId)
        {
            var balanceData = await _tradeService.GetBalanceData(userId);
            if (balanceData != null)
            {
                await Clients.User(userId.ToString()).SendAsync("ReceiveBalanceUpdate", balanceData);
            }
        }


        /// <summary>
        /// Sends an order update to a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>0
        public async Task SendOrderUpdate(Guid userId)
        {
            var orders = await _tradeService.GetOrderData(userId);
            if (orders != null)
            {
                await Clients.User(userId.ToString()).SendAsync("ReceiveOrderUpdate", orders);
            }
        }
    }
}
