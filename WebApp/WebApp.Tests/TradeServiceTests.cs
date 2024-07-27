using Xunit;
using Microsoft.EntityFrameworkCore;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Bd.Enums;
using System;
using WebApp.Utilities.Trading.TradingLobby;
using Moq;
using Microsoft.AspNetCore.SignalR;
using WebApp.Utilities.Trading;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Tests
{
    
    
/// <summary>
/// Those tests are not uo to date
/// </summary>
    public class TradeServiceTests
    {
        public TradeServiceTests()
        {
            // Set environment variable for testing
            Environment.SetEnvironmentVariable("TEST_ENVIRONMENT", "true");
        }

        [Fact]
        public async Task PlaceOrder_Should_Reduce_AvailableBalance_For_Buy_Order_With_Conversion()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName: "InMemoryDb")
                .Options;

            var userId = Guid.NewGuid();
            var symbol = "USD/CAD";
            var orderType = OrderType.Buy;
            var quantityInLots = 0.01f;
            var price = 1.2f; // Let's assume the price is 1.2

            using (var context = new Context(options))
            {
                var user = new AppUser("testuser")
                {
                    Id = userId,
                    //FirstName = "FirstName",
                   // LastName = "LastName",
                    VisibleName = "VisibleName",
                    Balance = new Bd.Infrastructure.Balance { AvailableBalance = 10000, TotalBalance = 10000 } // Initial balance of 10000 for both Available and Total
                };
                var stock = new Stock
                {
                    Name = symbol,
                    Ask = 1.25f, // Assume ask price for conversion
                    Bid = 1.2f, // Assume bid price
                    StockType = StockType.Forex,
                    Leverage = 100
                };

                context.Users.Add(user);
                context.Stocks.Add(stock);
                context.SaveChanges();
            }

            // Act
            var tradeService = CreateTradeService(options);
            await tradeService.PlaceOrder(userId, symbol, orderType, quantityInLots, price);

            // Assert
            using (var context = new Context(options))
            {
                var updatedUser = context.Users.Include(u => u.Balance).FirstOrDefault(u => u.Id == userId);
                var expectedCostInUnits = 1.25f;
                var expectedMargin = (quantityInLots * 100000 * expectedCostInUnits) / 100; // Applying conversion rate
                Assert.Equal(10000 - expectedMargin, updatedUser.Balance.AvailableBalance);
            }
        }

        [Fact]
        public async Task PlaceOrder_With_TakeProfit_Should_Complete_Order_On_Price_Hit()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName: "InMemoryDb")
                .Options;

            using (var context = new Context(options))
            {
                var userId = Guid.NewGuid();
                var symbol = "USD/CAD2";
                var orderType = OrderType.Buy;
                var quantityInLots = 0.01f;
                var price = 1.25f; // Initial price
                var takeProfit = 1.3f;

                var user = new AppUser("testuser")
                {
                    Id = userId,
                    //FirstName = "FirstName",
                   // LastName = "LastName",
                    VisibleName = "VisibleName",
                    Balance = new Bd.Infrastructure.Balance { AvailableBalance = 10000, TotalBalance = 10000 }
                };
                var stock = new Stock
                {
                    Name = symbol,
                    Ask = 1.25f,
                    Bid = 1.2f,
                    StockType = StockType.Forex,
                    Leverage = 100
                };

                context.Users.Add(user);
                context.Stocks.Add(stock);
                context.SaveChanges();

                // Act
                var tradeService = CreateTradeService(options);
                await tradeService.PlaceOrder(userId, symbol, orderType, quantityInLots, price, takeProfit: takeProfit);
                context.SaveChanges();

                // Assert initial margin reduction
                var initialMargin = (quantityInLots * 100000 * 1.25f) / 100;
                await VerifyUserBalance(options, userId, initialMargin, 0);
                context.SaveChanges();

                // Simulate price updates and verify balance changes
                await SimulatePriceUpdate(tradeService, options, symbol, 1.26f, 1.27f); // Price goes up
                await VerifyUserBalance(options, userId, initialMargin, (quantityInLots * 100000 * (1.26f - 1.25f)));
                context.SaveChanges();

                await SimulatePriceUpdate(tradeService, options, symbol, 1.28f, 1.29f); // Price goes up more
                await VerifyUserBalance(options, userId, initialMargin, (quantityInLots * 100000 * (1.28f - 1.25f)));
                context.SaveChanges();

                // Hits take profit
                await SimulatePriceUpdate(tradeService, options, symbol, 1.3f, 1.31f);
                context.SaveChanges();

                await VerifyOrderCompletionAndFinalBalance(options, userId, symbol, (quantityInLots * 100000 * (1.3f - 1.25f)));
            }
        }

        private async Task VerifyUserBalance(DbContextOptions<Context> options, Guid userId, float initialMargin, float expectedProfit)
        {
            using (var context = new Context(options))
            {
                var user = await context.Users.Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
                float expectedBalance = 10000 - initialMargin + expectedProfit;
                Assert.Equal(expectedBalance, user.Balance.AvailableBalance);
            }
        }


        private TradeService CreateTradeService(DbContextOptions<Context> options)
        {
            var contextFactory = new ContextFactory(options);
            var webSocketClientMock = new Mock<TwelveDataWebSocketClient>("fake-api-key");
            var apiClientMock = new Mock<TwelveDataApiClient>("fake-api-key");
            var hubContextMock = new Mock<IHubContext<TradeHub>>();

            return new TradeService(
                contextFactory,
                webSocketClientMock.Object,
                apiClientMock.Object,
                hubContextMock.Object,10);
        }
        private async Task SimulatePriceUpdate(TradeService tradeService, DbContextOptions<Context> options, string symbol, float bid, float ask)
        {
            using (var context = new Context(options))
            {
                var stock = context.Stocks.FirstOrDefault(s => s.Name == symbol);
                if (stock != null)
                {
                    stock.Bid = bid;
                    stock.Ask = ask;
                    context.SaveChanges();
                }
            }

            await tradeService.OnPriceUpdateAsync(symbol, bid, ask);
        }

        private async Task VerifyUserBalance(DbContextOptions<Context> options, Guid userId, float expectedBalance)
        {
            using (var context = new Context(options))
            {
                var updatedUser = await context.Users.Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
                Assert.Equal(expectedBalance, updatedUser.Balance.AvailableBalance);
            }
        }

        private async Task VerifyOrderCompletionAndFinalBalance(DbContextOptions<Context> options, Guid userId, string symbol, float expectedProfit)
        {
            using (var context = new Context(options))
            {
                var user = await context.Users.Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
                float expectedBalance = 10000 + expectedProfit;
                Assert.Equal(expectedBalance, user.Balance.AvailableBalance);

                var order = await context.Orders.FirstOrDefaultAsync(o => o.User.Id == userId && o.Stock.Name == symbol);
                Assert.Equal(OrderState.Completed, order.OrderState);
            }
        }

    }
}

    // Simple ContextFactory implementation
    public class ContextFactory : IDbContextFactory<Context>
    {
        private readonly DbContextOptions<Context> _options;

        public ContextFactory(DbContextOptions<Context> options)
        {
            _options = options;
        }

        public Context CreateDbContext()
        {
            return new Context(_options);
        }
    }

