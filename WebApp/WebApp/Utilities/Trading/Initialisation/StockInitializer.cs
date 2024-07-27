using Bd.Enums;
using Bd.Infrastructure;

namespace WebApp.Utilities.Trading.Initialisation
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Initializes stock data in the database if no stock data exists.
    /// </summary>
    public static class StockInitializer
    {
        /// <summary>
        /// Initializes the stock data in the given database context if no stock data exists.
        /// </summary>
        /// <param name="context">The database context to initialize the stock data in.</param>
        public static void InitializeStocks(Context context)
        {
            if (!context.Stocks.Any())
            {
                var stocks = new List<Stock>
            {
                new Stock
                {
                    Name = "BTC/USD",
                    StockType = StockType.Crypto,
                    Leverage = 2,
                    Bid = 0, // Example bid price
                    Ask = 0  // Example ask price
                },
                                new Stock
                {
                    Name = "USD/CAD",
                    StockType = StockType.Forex,
                    Leverage = 100,
                    Bid = 0, // Example bid price
                    Ask = 0  // Example ask price
                },
                // Uncomment and add more stocks as needed
                // new Stock
                // {
                //     Name = "GOLD/USD",
                //     StockType = StockType.Commodities,
                //     Leverage = 20,
                //     Bid = 0,
                //     Ask = 0
                // },
                // new Stock
                // {
                //     Name = "EUR/USD",
                //     StockType = StockType.Forex,
                //     Leverage = 100,
                //     Bid = 0,
                //     Ask = 0
                // }
            };

                context.Stocks.AddRange(stocks);
                context.SaveChanges();
            }
        }
    }

}
