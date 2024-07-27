using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Utilities.Trading;

namespace WebApp.Controllers
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: API Controller for handling batch data retrieval related to trading.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BatchController : ControllerBase
    {
        private readonly ITradeService _tradeService;

        /// <summary>
        /// Constructor to initialize the ITradeService.
        /// </summary>
        /// <param name="tradeService">Injected trade service.</param>
        public BatchController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        /// <summary>
        /// Endpoint to get batch data including orders, bid-ask data, and balance.
        /// </summary>
        /// <param name="symbol">The trading symbol.</param>
        /// <param name="interval">The interval for the data.</param>
        /// <returns>Batch data including orders, bid-ask data, and balance.</returns>
        [HttpGet("batch")]
        public async Task<IActionResult> GetBatchData(string symbol, string interval)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            var ordersTask = _tradeService.GetOrderData(userId);
            var bidAskTask = _tradeService.GetBidAskData(symbol);
            var balanceTask = _tradeService.GetBalanceData(userId);

            await Task.WhenAll(ordersTask, bidAskTask);
            return Ok(new
            {
                Orders = await ordersTask,
                BidAsk = await bidAskTask,
                Balance = await balanceTask
            });
        }
    }
}
