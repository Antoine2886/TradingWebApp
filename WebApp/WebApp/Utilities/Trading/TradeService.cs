using Bd.Infrastructure;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WebApp.Utilities.Trading;
using Microsoft.CodeAnalysis.Elfie.Model;
using System.Timers;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using NuGet.Packaging.Signing;
using System.Collections;
using WebApp.Utilities.Trading.Cache;
using WebApp.Utilities.Trading.Time;
using Microsoft.AspNetCore.SignalR;
using WebApp.Utilities.Trading.TradingLobby;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Utilities.Trading.DTO;
using System.Security.Cryptography;
using Bd.Enums;
using System.Threading.RateLimiting;
using NuGet.Packaging;


/// <summary>
/// Author: Antoine Bélanger
/// Description: Implementation of trade services and price updates. Manages WebSocket connections, data aggregation, and order handling.
/// </summary>
public class TradeService : ITradeService, IPriceObserver
    {
    private readonly IHubContext<TradeHub> _hubContext;
    private readonly IDbContextFactory<Context> _contextFactory;
    private readonly TwelveDataWebSocketClient _webSocketClient;
        private readonly TwelveDataApiClient _webApiClient;
    private readonly ConcurrentDictionary<string, RealTimePriceTracker> _priceTrackers = new ConcurrentDictionary<string, RealTimePriceTracker>();



    private SymbolManager _symbolManager;


    private readonly List<IPriceObserver> _priceObservers = new List<IPriceObserver>();
    private System.Timers.Timer _oneMinuteTimer;
    private System.Timers.Timer _threeMinuteTimer;
    private System.Timers.Timer _fiveMinuteTimer;
    private System.Timers.Timer _fifteenMinuteTimer;
    private System.Timers.Timer _thirtyMinuteTimer;
    private System.Timers.Timer _fortyFiveMinuteTimer;
    private System.Timers.Timer _hourlyTimer;
    private System.Timers.Timer _twoHourlyTimer;
    private System.Timers.Timer _fourHourlyTimer;
    private System.Timers.Timer _eightHourlyTimer;
    private System.Timers.Timer _dailyTimer;
    private System.Timers.Timer _weeklyTimer;
    private System.Timers.Timer _monthlyTimer;
    private readonly ConcurrentDictionary<string, List<PriceDataPoint>> _priceCache = new ConcurrentDictionary<string, List<PriceDataPoint>>();
    private List<string> _pendingIntervals = new List<string>();
    private readonly Queue<string> _symbolQueue;
    private readonly RateLimiter _rateLimiter;


    /// <summary>
    /// Initializes a new instance of the TradeService class.
    /// </summary>
    /// <param name="contextFactory">The factory to create database context instances.</param>
    /// <param name="webSocketClient">The WebSocket client for receiving real-time data.</param>
    /// <param name="webApiClient">The API client for fetching data.</param>
    /// <param name="hubContext">The SignalR hub context for sending updates to clients.</param>
    /// <param name="maxRequestsPerMinute">The maximum number of requests allowed per minute.</param>
    public TradeService(IDbContextFactory<Context> contextFactory, TwelveDataWebSocketClient webSocketClient, TwelveDataApiClient webApiClient, IHubContext<TradeHub> hubContext, int maxRequestsPerMinute = 10)
    {
            _contextFactory = contextFactory;
            _webSocketClient = webSocketClient;
            _webApiClient = webApiClient;
        _hubContext = hubContext;

        InitializeAggregationTimer();
        _rateLimiter = new RateLimiter(maxRequestsPerMinute);
        _symbolManager = new SymbolManager();
        _symbolQueue = new Queue<string>(_symbolManager.Symbols);

    }

    /// <summary>
    /// Registers an observer for price updates.
    /// </summary>
    /// <param name="observer">The observer to register.</param>
    public void RegisterObserver(IPriceObserver observer)
    {
        _priceObservers.Add(observer);
    }
    /// <summary>
    /// Removes an observer for price updates.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    public void RemoveObserver(IPriceObserver observer)
    {
        _priceObservers.Remove(observer);
    }

    /// <summary>
    /// Handles a price update for a given symbol.
    /// </summary>
    /// <param name="symbol">The symbol of the stock.</param>
    /// <param name="bid">The bid price.</param>
    /// <param name="ask">The ask price.</param>
    public async Task OnPriceUpdateAsync(string symbol, float bid, float ask)
    {
        using var context = _contextFactory.CreateDbContext();
        var pendingOrders = await context.Orders
            .Include(o => o.User).ThenInclude(u => u.Balance)
            .Include(o => o.Stock)
            .Where(o => o.Stock.Name == symbol && (o.OrderState == OrderState.Pending || o.OrderState == OrderState.Opened))
            .ToListAsync();

        foreach (var order in pendingOrders)
        {
            try
            {
                if (order.OrderState == OrderState.Pending)
                {
                    bool shouldOpen = false;
                    switch (order.OrderType)
                    {
                        case OrderType.BuyLimit:
                            shouldOpen = ask <= order.Price;
                            break;
                        case OrderType.BuyStop:
                            shouldOpen = ask >= order.Price;
                            break;
                        case OrderType.SellLimit:
                            shouldOpen = bid >= order.Price;
                            break;
                        case OrderType.SellStop:
                            shouldOpen = bid <= order.Price;
                            break;
                    }

                    if (shouldOpen)
                    {
                        await OpenPendingOrder(order.Id, order.User.Id);
                    }
                }
                else if (order.OrderState == OrderState.Opened)
                {
                    var currentPrice = order.OrderType == OrderType.Buy || order.OrderType == OrderType.BuyLimit || order.OrderType == OrderType.BuyStop ? bid : ask;

                    // Calculate current conversion rate if Forex
                    float currentConversionRate = 1;
                    if (order.Stock.StockType == StockType.Forex)
                    {

                        currentConversionRate = await GetConversionRate(order.Stock.Name, order.Stock.StockType);
                    }

                    // Calculate current value
                    float currentValue = 0;
                    switch (order.Stock.StockType)
                    {
                        case StockType.Forex:
                            currentValue = order.QuantityInLots * 100000 * currentPrice * currentConversionRate;
                            break;
                        case StockType.Crypto:
                            currentValue = order.QuantityInLots * currentPrice;
                            break;
                        case StockType.Commodities:
                            currentValue = order.QuantityInLots * currentPrice;
                            break;
                    }

                    // Calculate profit/loss
                    float profitOrLoss = 0;
                    if (order.OrderType == OrderType.Buy || order.OrderType == OrderType.BuyLimit || order.OrderType == OrderType.BuyStop)
                    {
                        profitOrLoss = currentValue - order.InitialCost;
                    }
                    else if (order.OrderType == OrderType.Short || order.OrderType == OrderType.SellLimit || order.OrderType == OrderType.SellStop)
                    {
                        profitOrLoss = order.InitialCost - currentValue;
                    }

                    // Update the order with the calculated profit/loss
                    order.ProfitOrLoss = profitOrLoss;

                    // Check if the order should be executed based on take profit or stop loss
                    bool executeOrder = false;

                    if (order.TakeProfit.HasValue && (
                        ((order.OrderType == OrderType.Buy || order.OrderType == OrderType.BuyLimit || order.OrderType == OrderType.BuyStop) && currentPrice >= order.TakeProfit.Value) ||
                        ((order.OrderType == OrderType.Short || order.OrderType == OrderType.SellLimit || order.OrderType == OrderType.SellStop) && currentPrice <= order.TakeProfit.Value)))
                    {
                        executeOrder = true;
                    }

                    if (order.StopLoss.HasValue && (
                        ((order.OrderType == OrderType.Buy || order.OrderType == OrderType.BuyLimit || order.OrderType == OrderType.BuyStop) && currentPrice <= order.StopLoss.Value) ||
                        ((order.OrderType == OrderType.Short || order.OrderType == OrderType.SellLimit || order.OrderType == OrderType.SellStop) && currentPrice >= order.StopLoss.Value)))
                    {
                        executeOrder = true;
                    }

                    if (executeOrder)
                    {
                        await ExecuteOrder(order, order.User.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                // Optionally, log exceptions
            }
        }

        // Calculate and update available balance for each user with open orders
        var userBalanceUpdates = pendingOrders
            .Where(o => o.OrderState == OrderState.Opened)  // Filter only opened orders
            .GroupBy(o => o.User.Id)
            .Select(g => new
            {
                UserId = g.Key,
                ProfitOrLoss = g.Sum(o => o.ProfitOrLoss),
                TotalMargin = g.Sum(o => o.Margin)
            })
            .ToList();

        foreach (var userBalanceUpdate in userBalanceUpdates)
        {
            var user = await context.Users
                .Include(u => u.Balance)
                .FirstOrDefaultAsync(u => u.Id == userBalanceUpdate.UserId);

            if (user != null)
            {
                user.Balance.AvailableBalance = user.Balance.TotalBalance + userBalanceUpdate.ProfitOrLoss - userBalanceUpdate.TotalMargin;
            }
        }

        await context.SaveChangesAsync();
    }


    /// <summary>
    /// Initializes the aggregation timers for various intervals.
    /// </summary>
    public void InitializeAggregationTimer()
    {
        InitializeTimer(ref _oneMinuteTimer, "1min", 1);
        InitializeTimer(ref _fiveMinuteTimer, "5min", 5);
        InitializeTimer(ref _fifteenMinuteTimer, "15min", 15);
        InitializeTimer(ref _thirtyMinuteTimer, "30min", 30);
        InitializeTimer(ref _fortyFiveMinuteTimer, "45min", 45);
        InitializeTimer(ref _hourlyTimer, "1h", 60);
        InitializeTimer(ref _twoHourlyTimer, "2h", 120);
        InitializeTimer(ref _fourHourlyTimer, "4h", 240);
        InitializeTimer(ref _eightHourlyTimer, "8h", 480);
        InitializeTimer(ref _dailyTimer, "1D", 1440);
        InitializeTimer(ref _weeklyTimer, "1W", 10080);
        InitializeTimer(ref _monthlyTimer, "1M", 43200);
    }

    /// <summary>
    /// Initializes a timer for a specific interval.
    /// </summary>
    /// <param name="timer">The timer to initialize.</param>
    /// <param name="interval">The interval string.</param>
    /// <param name="minutes">The interval duration in minutes.</param>
    private void InitializeTimer(ref System.Timers.Timer timer, string interval, int minutes)
    {
        double intervalMilliseconds;

        if (interval == "1M" || interval == "1W" || interval == "1D")
        {
            intervalMilliseconds = GetTimeUntilNextSpecialInterval(interval);
        }
        else
        {
            intervalMilliseconds = GetTimeUntilNextInterval(minutes);
        }


    }


    /// <summary>
    /// Calculates the time until the next special interval (day, week, month).
    /// </summary>
    /// <param name="interval">The interval string.</param>
    /// <returns>The time until the next special interval in milliseconds.</returns>
    public static double GetTimeUntilNextSpecialInterval(string interval)
    {
        DateTime now = TimeHelper.GetEasternTime();
        DateTime nextInterval;

        switch (interval)
        {
            case "1D":
                nextInterval = now.Date.AddDays(1);
                break;
            case "1W":
                int daysToAdd = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
                nextInterval = now.Date.AddDays(daysToAdd).AddHours(-now.Hour).AddMinutes(-now.Minute).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
                break;
            case "1M":
                nextInterval = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                break;
            default:
                throw new ArgumentException($"Unsupported interval: {interval}");
        }

        return (nextInterval - now).TotalMilliseconds;
    }

    /// <summary>
    /// Sends a balance update to a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    public async Task SendBalanceUpdate(Guid userId)
    {
        var balanceData = await GetBalanceData(userId);
        if (balanceData != null)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveBalanceUpdate", balanceData);
        }
    }

    /// <summary>
    /// Gets the balance data for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The balance data for the user.</returns>
    public async Task<BalanceDto> GetBalanceData(Guid userId)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        return new BalanceDto
        {
            TotalBalance = user.Balance.TotalBalance,
            AvailableBalance = user.Balance.AvailableBalance
        };
    }

    /// <summary>
    /// Gets the order data for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of order data for the user.</returns>
    public async Task<List<OrderDto>> GetOrderData(Guid userId)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Include(u => u.Orders).ThenInclude(o => o.Stock).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        return user.Orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            Symbol = o.Stock.Name, // Updated to use Stock's Name
            Quantity = o.QuantityInLots,
            Price = o.Price,
            OrderType = o.OrderType,
            OrderState = o.OrderState, // New property
            StopLoss = o.StopLoss, // New property
            TakeProfit = o.TakeProfit, // New property
            ProfitorLoss = o.ProfitOrLoss, // New property
            ClosedPrice = o.ClosedPrice,
        }).ToList();
    }

    /// <summary>
    /// Handles the elapsed event of an aggregation timer.
    /// </summary>
    /// <param name="interval">The interval string.</param>
    /// <param name="minutes">The interval duration in minutes.</param>
    private async Task OnAggregationTimerElapsed(string interval, int minutes)
    {
        if (interval == "1min" || interval == "5min" || interval == "15min")
        {
            await FetchAndStoreTimeSeriesData(interval);
        }

        foreach (var symbol in _symbolManager.Symbols)
        {
            using var context = _contextFactory.CreateDbContext();
            AggregateIntervalData(symbol, interval, context); // Aggregate for the specified interval
        }

        RestartTimer(interval, minutes);
    }

    private void RestartTimer(string interval, int minutes)
    {
        switch (interval)
        {
            case "1min":
                _oneMinuteTimer.Interval = GetTimeUntilNextInterval(1);
                _oneMinuteTimer.Start();
                break;
            case "5min":
                _fiveMinuteTimer.Interval = GetTimeUntilNextInterval(5);
                _fiveMinuteTimer.Start();
                break;
            case "15min":
                _fifteenMinuteTimer.Interval = GetTimeUntilNextInterval(15);
                _fifteenMinuteTimer.Start();
                break;
            case "30min":
                _thirtyMinuteTimer.Interval = GetTimeUntilNextInterval(30);
                _thirtyMinuteTimer.Start();
                break;
            case "45min":
                _fortyFiveMinuteTimer.Interval = GetTimeUntilNextInterval(45);
                _fortyFiveMinuteTimer.Start();
                break;
            case "1h":
                _hourlyTimer.Interval = GetTimeUntilNextInterval(60);
                _hourlyTimer.Start();
                break;
            case "2h":
                _twoHourlyTimer.Interval = GetTimeUntilNextInterval(120);
                _twoHourlyTimer.Start();
                break;
            case "4h":
                _fourHourlyTimer.Interval = GetTimeUntilNextInterval(240);
                _fourHourlyTimer.Start();
                break;
            case "8h":
                _eightHourlyTimer.Interval = GetTimeUntilNextInterval(480);
                _eightHourlyTimer.Start();
                break;
            case "1D":
                _dailyTimer.Interval = GetTimeUntilNextInterval(1440);
                _dailyTimer.Start();
                break;
            case "1W":
                _weeklyTimer.Interval = GetTimeUntilNextInterval(10080);
                _weeklyTimer.Start();
                break;
            case "1M":
                _monthlyTimer.Interval = GetTimeUntilNextInterval(43200);
                _monthlyTimer.Start();
                break;
        }
    }



    /// <summary>
    /// Fetches and stores time series data for a specific interval.
    /// </summary>
    /// <param name="interval">The interval string.</param>
    private async Task FetchAndStoreTimeSeriesData(string interval)
    {
        using var context = _contextFactory.CreateDbContext();
        foreach (var symbol in _symbolManager.Symbols)
        {

            DateTime startDate = CalculateStartDate(interval);
            var testArray = await _webApiClient.GetTimeSeriesSmallOutput(symbol, interval, startDate);

            if (testArray == null || !testArray.Any())
            {
                Console.WriteLine($"No data returned from the API for {symbol} at interval {interval}.");
                continue;
            }

            try
            {
                var existingStock = await context.Stocks.Include(s => s.StockData).FirstOrDefaultAsync(s => s.Name == symbol);

                if (existingStock != null)
                {
                    foreach (var item in testArray)
                    {
                        var stockData = item.ToObject<StockData>();
                        stockData.Interval = interval;
                        var timeStamp = DateTime.ParseExact(item["datetime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        var existingStockData = context.StockData.FirstOrDefault(e => e.Stock == existingStock && e.TimeStamp == timeStamp && e.Interval == interval);

                        if (existingStockData != null)
                        {
                            existingStockData.Open = stockData.Open;
                            existingStockData.Close = stockData.Close;
                            existingStockData.High = stockData.High;
                            existingStockData.Low = stockData.Low;
                        }
                        else
                        {
                            stockData.TimeStamp = timeStamp;
                            existingStock.StockData.Add(stockData);
                        }

                    }
                }
                else
                {
                    var newStock = new Stock
                    {
                        Name = symbol,
                        StockData = new List<StockData>()
                    };

                    foreach (var item in testArray)
                    {
                        var stockData = item.ToObject<StockData>();
                        stockData.Interval = interval;
                        var timeStamp = DateTime.ParseExact(item["datetime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        stockData.TimeStamp = timeStamp;
                        newStock.StockData.Add(stockData);
                    }

                    context.Stocks.Add(newStock);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stocks for {symbol} at interval {interval}: {ex.Message}");
            }
        }
    }




    /// <summary>
    /// Connects to the WebSocket and subscribes to symbols.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConnectWebSocket()
        {
            try
            {
                await _webSocketClient.Connect();

                await _webSocketClient.SendSubscribeEvent(_symbolManager.Symbols);




              // IMPORTANT, THIS IS TO LOAD THE DATA FROM SYMBOL MANAGER -> YOU NEED AT LEAST THE FIRST PAID TIER OF TWELVEDATA
              //  await InitializeDataLoading();  



            // Continue listening for messages until the WebSocket connection is closed
            while (_webSocketClient.IsConnected)
                {
                    // Receive a message from the WebSocket connection
                    var message = await _webSocketClient.ReceiveMessage();

                    await HandleWebSocketMessage(message);
                    // Process the received message as needed (e.g., display or log it)
                    Console.WriteLine($"Received message: {message}");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Failed to connect to WebSocket: {ex.Message}");
            }
        }
    /// <summary>
    /// Initializes data loading for all symbols.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeDataLoading()
    {
        var intervals = new List<string>
    {
        "1min", "5min", "15min", "30min",
        "45min", "1h", "2h", "4h",
        "8h", "1D", "1W", "1M"
    };

        while (_symbolQueue.Count > 0)
        {
            var symbol = _symbolQueue.Dequeue();

            // Check the current API usage
            var (usedCredits, remainingCredits) = await _webApiClient.GetApiUsageAsync();

            if (remainingCredits > 12)
            {
                await LoadDataForAllTimeframes(symbol, intervals);
            }
            else
            {
                // Delay until more credits are available
                Console.WriteLine("Not enough credits, delaying...");
                await Task.Delay(60000); // Adjust the delay as needed
                _symbolQueue.Enqueue(symbol); // Requeue the symbol for later processing
            }
        }
    }

    /// <summary>
    /// Updates stock data for a given symbol and interval.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="interval">The interval string.</param>
    /// <param name="endDate">The end date for the data retrieval.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateStocks(string name, string interval, DateTime? endDate = null)
    {
        using var context = _contextFactory.CreateDbContext();
        int iterationCount = 0;

        while (iterationCount < 1) // Chhange this number to 2 and it add 5k more data 
        {
            var testArray = await _webApiClient.GetTimeSeriesEndDate(name, interval, endDate: null);

            if (testArray == null || !testArray.Any())
            {
                Console.WriteLine("No data returned from the API or the response is null.");
                return;
            }

            try
            {
                var existingStock = await context.Stocks.Include(s => s.StockData).FirstOrDefaultAsync(s => s.Name == name);

                if (existingStock != null)
                {
                    var newStockDataList = new List<StockData>();
                    foreach (var item in testArray)
                    {
                        var stockData = item.ToObject<StockData>();
                        stockData.Interval = interval;
                        // Try parsing date-time format first
                        bool isParsed = DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeStamp);

                        // If parsing date-time fails, try parsing date-only format
                        if (!isParsed)
                        {
                            isParsed = DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeStamp);
                        }

                        if (isParsed)
                        {
                            var existingData = existingStock.StockData.FirstOrDefault(sd => sd.TimeStamp == timeStamp && sd.Interval == interval);
                            if (existingData == null)
                            {
                                stockData.TimeStamp = timeStamp;
                                newStockDataList.Add(stockData);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse date for item: {item}");
                        }
                    }
                    if (newStockDataList.Any())
                    {
                        existingStock.StockData.AddRange(newStockDataList);
                    }
                }
                else
                {
                    var newStock = new Stock
                    {
                        Name = name,
                        StockData = new List<StockData>()
                    };

                    foreach (var item in testArray)
                    {
                        var stockData = item.ToObject<StockData>();
                        stockData.Interval = interval;
                        // Try parsing date-time format first
                        bool isParsed = DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeStamp);

                        // If parsing date-time fails, try parsing date-only format
                        if (!isParsed)
                        {
                            isParsed = DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeStamp);
                        }

                        if (isParsed)
                        {
                            var existingData = existingStock.StockData.FirstOrDefault(sd => sd.TimeStamp == timeStamp && sd.Interval == interval);
                            if (existingData == null)
                            {
                                stockData.TimeStamp = timeStamp;
                                existingStock.StockData.Add(stockData);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse date for item: {item}");
                        }

                        stockData.TimeStamp = timeStamp;
                        newStock.StockData.Add(stockData);
                    }

                    context.Stocks.Add(newStock);
                }

                await context.SaveChangesAsync();

                var oldestTimestamp = testArray.Min(item =>
                {
                    // Try parsing date-time format first
                    if (DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                    {
                        return dateTime;
                    }
                    // If parsing date-time fails, try parsing date-only format
                    else if (DateTime.TryParseExact(item["datetime"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                    {
                        return dateTime;
                    }
                    return DateTime.MaxValue; // Return a maximum date in case of parsing failure
                });

                // Define a buffer period based on the interval
                var intervalBuffer = interval switch
                {
                    "1min" => TimeSpan.FromMinutes(1),
                    "5min" => TimeSpan.FromMinutes(5),
                    "15min" => TimeSpan.FromMinutes(15),
                    "30min" => TimeSpan.FromMinutes(30),
                    "45min" => TimeSpan.FromMinutes(45),
                    "1h" => TimeSpan.FromHours(1),
                    "2h" => TimeSpan.FromHours(2),
                    "4h" => TimeSpan.FromHours(4),
                    "8h" => TimeSpan.FromHours(8),
                    "1D" => TimeSpan.FromDays(1),
                    "1W" => TimeSpan.FromDays(7),
                    "1M" => TimeSpan.FromDays(30),
                    _ => TimeSpan.FromMinutes(1)
                };
                var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var currentEasternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternTimeZone);
                var flooredEasternTime = FloorToNearestInterval(currentEasternTime, interval) - TimeSpan.FromMinutes(1);

                // Update endDate for the next iteration to go further back in time
                var currentEndDate = oldestTimestamp;
                iterationCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stocks: {ex.Message}");
            }
        }
    }




    /// <summary>
    /// Loads data for all timeframes for a given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to load data for.</param>
    /// <param name="intervals">The list of intervals to load data for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadDataForAllTimeframes(string symbol, List<string> intervals)
    {
        foreach (var interval in intervals)
        {
            //DateTime? earliestTimestamp = await _webApiClient.GetEarliestTimestamp(symbol, interval);

            await UpdateStocks(symbol, interval);

        }
    }

    /// <summary>
    /// Handles a message received from the WebSocket.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>0
    public async Task HandleWebSocketMessage(string message)
    {
        try
        {
            // Parse WebSocket message
            JObject data = JObject.Parse(message);
            string symbol = data["symbol"]?.ToString();
            var priceToken = data["price"]; // Assuming price is sent as "price"
            var bidToken = data["bid"]; // Assuming bid is sent as "bid"
            var askToken = data["ask"]; // Assuming ask is sent as "ask"
            var timestampToken = data["timestamp"]; // Assuming timestamp is sent as "timestamp"

            if (symbol == null || priceToken == null || timestampToken == null)
            {
                Console.WriteLine("Invalid message format. Skipping message.");
                return;
            }

            // Parse price and timestamp
            float currentPrice = float.Parse(priceToken.ToString(), CultureInfo.InvariantCulture);
            long timestampUnix = long.Parse(timestampToken.ToString(), CultureInfo.InvariantCulture);


            float bidPrice = 0;
            float askPrice = 0;

            if (bidToken != null)
            {
                 bidPrice = float.Parse(bidToken.ToString(), CultureInfo.InvariantCulture);
            }
            if (askToken != null)
            {
                askPrice = float.Parse(askToken.ToString(), CultureInfo.InvariantCulture);

            }

            long adjustedTimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (4 * 3600);
            DateTime adjustedDateTime = DateTimeOffset.FromUnixTimeSeconds(adjustedTimestampUnix).UtcDateTime;


            long  lutcDateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (4 * 3600);
            DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(lutcDateTime).UtcDateTime;
            utcDateTime = FloorToNearestMinute(utcDateTime);



            using var context = _contextFactory.CreateDbContext();
            var existingStock = context.Stocks.Include(s => s.StockData).FirstOrDefault(s => s.Name == symbol);


            if (existingStock != null && bidPrice != 0 && askPrice != 0)
            {
                existingStock.Bid = bidPrice;
                existingStock.Ask = askPrice;
            }
            await OnPriceUpdateAsync(symbol, bidPrice, askPrice);

            await context.SaveChangesAsync();

            // Store to local cache for minute aggregation
            StoreToCache(symbol, adjustedDateTime, currentPrice);




            // Save the latest data to the database
            SaveLatestDataToDatabase(symbol, utcDateTime, currentPrice);


            await _hubContext.Clients.All.SendAsync("ReceiveTradeUpdate", Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                symbol,
                price = currentPrice,
                bid = bidPrice,
                ask = askPrice,
                adjustedTimestampUnix = timestampUnix
            }));
        

            // Update real-time price tracker
            _priceTrackers.AddOrUpdate(symbol, new RealTimePriceTracker
                {
                    CurrentPrice = currentPrice,
                    LastUpdated = utcDateTime
                },
                (key, tracker) =>
            {
                tracker.CurrentPrice = currentPrice;
                tracker.LastUpdated = utcDateTime;
                return tracker;
            });
            // Get the users related to this symbol and send balance updates
            var usersWithSymbol = await context.Users
                .Include(u => u.Orders)
                .Where(u => u.Orders.Any(o => o.Stock.Name == symbol && o.OrderState == OrderState.Opened))
                .ToListAsync();

            foreach (var user in usersWithSymbol)
            {
                await SendBalanceUpdate(user.Id);
                await SendOrderUpdate(user.Id); // Sending order updates
            }


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling WebSocket message: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an order update to a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendOrderUpdate(Guid userId)
    {
        List<OrderDto> orders = await GetOrderData(userId);
        if (orders != null)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveOrderUpdate", orders);
        }
    }
    /// <summary>
    /// Saves the latest data to the database for a specific symbol and timestamp.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="price">The price.</param>
    private void SaveLatestDataToDatabase(string symbol, DateTime timestamp, float price)
    {
        using var context = _contextFactory.CreateDbContext();
        var existingStock = context.Stocks.Include(s => s.StockData).FirstOrDefault(s => s.Name == symbol);

        var intervals = new List<string>
    {
        "1min", "5min", "15min", "30min",
        "45min", "1h", "2h", "4h",
        "8h", "1D", "1W", "1M"
    };

        foreach (var interval in intervals)
        {
            var flooredTimestamp = FloorToNearestInterval(timestamp, interval);

            if (existingStock != null)
            {
                var existingData = existingStock.StockData.FirstOrDefault(sd => sd.TimeStamp == flooredTimestamp && sd.Interval == interval);
                if (existingData != null)
                {
                    existingData.High = Math.Max(existingData.High, price);
                    existingData.Low = Math.Min(existingData.Low, price);
                    existingData.Close = price;
                }
                else
                {
                    var newStockData = new StockData
                    {
                        TimeStamp = flooredTimestamp,
                        Open = price,
                        High = price,
                        Low = price,
                        Close = price,
                        Interval = interval,
                        Stock = existingStock
                    };
                    context.StockData.Add(newStockData);
                }
            }
        }

        context.SaveChanges();
    }










    /// <summary>
    /// Stores price data to the local cache.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="price">The price.</param>
    private void StoreToCache(string symbol, DateTime timestamp, float price)
    {
        var dataPoint = new PriceDataPoint
        {
            Timestamp = timestamp,
            Price = price
        };

        _priceCache.AddOrUpdate(symbol,
            new List<PriceDataPoint> { dataPoint },
            (key, existingList) =>
            {
                lock (existingList) // Ensure thread safety
                {
                    existingList.Add(dataPoint);
                    return existingList;
                }
            });
    }
    /// <summary>
    /// Gets the bid and ask data for a specific symbol.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <returns>The bid and ask data.</returns>0
    public async Task<BidAskDto> GetBidAskData(string symbol)
    {
        using var context = _contextFactory.CreateDbContext();

        var stock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == symbol);
        if (stock == null)
        {
            var dto = new BidAskDto
            {
                Bid = 0,
                Ask = 0
            };
            return dto;

        }
        else
        {
            var dto = new BidAskDto
            {
                Bid = stock.Bid,
                Ask = stock.Ask
            };
            return dto;

        }
    }

    /// <summary>
    /// Gets the latest data for a specific symbol and interval.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="interval">The interval string.</param>
    /// <returns>The latest data.</returns>
    public async Task<StockDataDto> GetLatestData(string symbol, string interval)
    {
        using var context = _contextFactory.CreateDbContext();

        var latestData = await context.StockData
            .Where(sd => sd.Stock.Name == symbol && sd.Interval == interval)
            .OrderByDescending(sd => sd.TimeStamp)
            .FirstOrDefaultAsync();

        if (latestData == null)
        {
            return null;
        }

        var dto = new StockDataDto
        {
            Symbol = symbol,
            TimeStamp = new DateTimeOffset(latestData.TimeStamp).ToUnixTimeSeconds(),
            Open = latestData.Open,
            High = latestData.High,
            Low = latestData.Low,
            Close = latestData.Close,
            Interval = latestData.Interval
        };

        return dto;
    }


    /// <summary>
    /// Aggregates interval data for a specific symbol and interval.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="interval">The interval string.</param>
    /// <param name="context">The database context.</param>
    private void AggregateIntervalData(string symbol, string interval, Context context)
    {
        if (interval == "1min")
        {
            if (_priceCache.TryGetValue(symbol, out var priceList))
            {
                lock (priceList) // Ensure thread safety
                {
                    if (priceList.Any())
                    {
                        var open = priceList.First().Price;
                        var close = priceList.Last().Price;
                        var high = priceList.Max(p => p.Price);
                        var low = priceList.Min(p => p.Price);
                        var timestamp = FloorToNearestMinute(DateTime.UtcNow).AddHours(-4); // Use current time adjusted for 4-hour offset
                        long adjustedTimestampUnix = new DateTimeOffset(timestamp).ToUnixTimeSeconds();

                        var stock = context.Stocks.FirstOrDefault(s => s.Name == symbol);

                        if (stock != null)
                        {
                            // Check if the record already exists
                            var existingRecord = context.StockData
                                .FirstOrDefault(sd => sd.TimeStamp == timestamp && sd.Interval == interval && sd.Stock.Name == stock.Name);

                            if (existingRecord == null)
                            {
                                var aggregatedData = new StockData
                                {
                                    TimeStamp = timestamp,
                                    Open = open,
                                    High = high,
                                    Low = low,
                                    Close = close,
                                    Interval = interval,
                                    Stock = stock,
                                };
                                context.StockData.Add(aggregatedData);

                                var dto = new StockDataDto
                                {
                                    Symbol = stock.Name,
                                    TimeStamp = adjustedTimestampUnix,
                                    Open = aggregatedData.Open,
                                    High = aggregatedData.High,
                                    Low = aggregatedData.Low,
                                    Close = aggregatedData.Close,
                                    Interval = aggregatedData.Interval
                                };

                                _hubContext.Clients.All.SendAsync("NewCandle", Newtonsoft.Json.JsonConvert.SerializeObject(dto));
                            }
                        }
                    }
                }
            }
        }
        else
        {

                AggregateHigherIntervalData(symbol, interval, context);
        }
    }
    /// <summary>
    /// Aggregates higher interval data for a specific symbol and interval.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="interval">The interval string.</param>
    /// <param name="context">The database context.</param>
    private void AggregateHigherIntervalData(string symbol, string interval, Context context)
    {
        int intervalMinutes = interval switch
        {
            "5min" => 5,
            "15min" => 15,
            "30min" => 30,
            "45min" => 45,
            "1h" => 60,
            "2h" => 120,
            "4h" => 240,
            "8h" => 480,
            "1D" => 1440,
            "1W" => 10080,
            "1M" => 43200,
            _ => throw new ArgumentException("Unsupported interval")
        };

        string baseInterval = interval switch
        {
            "1D" => "1h",  // Use 1 hour data for daily aggregation
            "1W" => "4h",  // Use 4 hour data for weekly aggregation
            "1M" => "1D",  // Use daily data for monthly aggregation
            _ => "1min"    // Default to 1 minute data
        };

        var endTime = DateTime.UtcNow.AddHours(-4); // Adjust for Eastern Time Zone
        var startTime = endTime.AddMinutes(-intervalMinutes);

        var recentData = context.StockData
            .Where(sd => sd.Stock.Name == symbol && sd.Interval == baseInterval && sd.TimeStamp >= startTime && sd.TimeStamp <= endTime)
            .ToList();

        if (recentData.Any())
        {
            var open = recentData.First().Open;
            var close = recentData.Last().Close;
            var high = recentData.Max(p => p.High);
            var low = recentData.Min(p => p.Low);
            var timestamp = FloorToNearestInterval(recentData.First().TimeStamp, interval);

            var stock = context.Stocks.FirstOrDefault(s => s.Name == symbol);

            if (stock != null)
            {
                var existingRecord = context.StockData
                    .FirstOrDefault(sd => sd.TimeStamp == timestamp && sd.Interval == interval && sd.Stock == stock);

                if (existingRecord == null)
                {
                    var aggregatedData = new StockData
                    {
                        TimeStamp = timestamp,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Interval = interval,
                        Stock = stock,
                    };
                    context.StockData.Add(aggregatedData);

                    var dto = new StockDataDto
                    {
                        Symbol = stock.Name,
                        TimeStamp = new DateTimeOffset(timestamp).ToUnixTimeSeconds(),
                        Open = aggregatedData.Open,
                        High = aggregatedData.High,
                        Low = aggregatedData.Low,
                        Close = aggregatedData.Close,
                        Interval = aggregatedData.Interval
                    };

                    _hubContext.Clients.All.SendAsync("NewCandle", Newtonsoft.Json.JsonConvert.SerializeObject(dto));
                }
            }
            else
            {
                var newStock = new Stock
                {
                    Name = symbol
                };

                var aggregatedData = new StockData
                {
                    TimeStamp = timestamp,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Interval = interval,
                    Stock = newStock
                };

                var existingRecord = context.StockData
                    .FirstOrDefault(sd => sd.TimeStamp == timestamp && sd.Interval == interval && sd.Stock.Name == newStock.Name);

                if (existingRecord == null)
                {
                    context.StockData.Add(aggregatedData);

                    var dto = new StockDataDto
                    {
                        Symbol = newStock.Name,
                        TimeStamp = new DateTimeOffset(timestamp).ToUnixTimeSeconds(),
                        Open = aggregatedData.Open,
                        High = aggregatedData.High,
                        Low = aggregatedData.Low,
                        Close = aggregatedData.Close,
                        Interval = aggregatedData.Interval
                    };

                    _hubContext.Clients.All.SendAsync("NewCandle", Newtonsoft.Json.JsonConvert.SerializeObject(dto));
                }
            }

            context.SaveChanges();
        }
    }




    /// <summary>
    /// Floors a DateTime to the nearest interval.
    /// </summary>
    /// <param name="time">The DateTime to floor.</param>
    /// <param name="interval">The interval string.</param>
    /// <returns>The floored DateTime.</returns>
    private DateTime FloorToNearestInterval(DateTime time, string interval)
    {
        switch (interval)
        {
            case "1min":
                return TimeHelper.FloorToNearestMinute(time);
            case "3min":
                return TimeHelper.FloorToNearestThreeMinutes(time);
            case "5min":
                return TimeHelper.FloorToNearestFiveMinutes(time);
            case "15min":
                return TimeHelper.FloorToNearestFifteenMinutes(time);
            case "30min":
                return TimeHelper.FloorToNearestThirtyMinutes(time);
            case "45min":
                return TimeHelper.FloorToNearestFortyFiveMinutes(time);
            case "1h":
                return TimeHelper.FloorToNearestHour(time);
            case "2h":
                return TimeHelper.FloorToNearestTwoHours(time);
            case "4h":
                return TimeHelper.FloorToNearestFourHours(time);
            case "8h":
                return TimeHelper.FloorToNearestEightHours(time);
            case "1D":
                return TimeHelper.FloorToNearestDay(time);
            case "1W":
                return TimeHelper.FloorToNearestWeek(time);
            case "1M":
                return TimeHelper.FloorToNearestMonth(time);
            default:
                throw new ArgumentException(interval);
        }
    }

    /// <summary>
    /// Gets the conversion rate for a specific symbol and stock type. *** YOU NEED TO HAVE IT THE BD FOR IT TO WORK
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="stockType">The stock type.</param>
    /// <returns>The conversion rate.</returns>
    private async Task<float> GetConversionRate(string symbol, StockType stockType)
    {
        float conversionRate = 1;

        if (stockType == StockType.Forex)
        {
            var fromCurrency = symbol.Split('/')[0];
            var toCurrency = symbol.Split('/')[1];

            if (fromCurrency == "USD")
            {
                conversionRate = 1;
            }
            else if (toCurrency == "USD")
            {
                var usdToTo = await GetBidPriceForSymbol(symbol);
                conversionRate = 1 / usdToTo;
            }
            else
            {
                var usdToFrom = await GetBidPriceForSymbol($"USD/{fromCurrency}");
                var usdToTo = await GetBidPriceForSymbol($"USD/{toCurrency}");
                conversionRate = usdToTo / usdToFrom;
            }
        }

        return conversionRate;
    }




    /// <summary>
    /// Places an order for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="symbol">The symbol name.</param>
    /// <param name="orderType">The order type.</param>
    /// <param name="quantityInLots">The quantity in lots.</param>
    /// <param name="price">The price.</param>
    /// <param name="stopLoss">The stop loss price.</param>
    /// <param name="takeProfit">The take profit price.</param>
    /// <param name="orderState">The state of the order.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PlaceOrder(Guid userId, string symbol, OrderType orderType, float quantityInLots, float price, float? stopLoss = null, float? takeProfit = null, OrderState orderState = OrderState.Opened)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Include(u => u.Orders).Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
        var stock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == symbol);

        if (user == null || stock == null)
        {
            throw new Exception("User or Stock not found");

        }

        float conversionRate = 1;

        if (stock.StockType == StockType.Forex)
        {
            conversionRate = await GetConversionRate(symbol, stock.StockType);
        }



        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderType = orderType,
            QuantityInLots = quantityInLots,
            Price = price,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            OrderState = orderState,
            Stock = stock,
            User = user,
            OrderDate = DateTime.UtcNow,

        };

        context.Orders.Add(order);
        user.Orders.Add(order);

        if (orderState != OrderState.Pending)
        {
            var currentPrice = orderType == OrderType.Buy || orderType == OrderType.BuyLimit || orderType == OrderType.BuyStop ? stock.Ask : stock.Bid;

            // Calculate position value and required margin based on stock type
            float positionValue = 0;
            float leverage = stock.Leverage;

            switch (stock.StockType)
            {
                case StockType.Forex:
                    positionValue = quantityInLots * 100000 * currentPrice * conversionRate;
                    break;
                case StockType.Crypto:
                    positionValue = quantityInLots * currentPrice;
                    break;
                case StockType.Commodities:
                    positionValue = quantityInLots * currentPrice;
                    break;
                default:
                    throw new Exception("Unknown Stock Type");
            }

            var requiredMargin = positionValue / leverage;

            if (user.Balance.AvailableBalance < requiredMargin)
            {
                throw new Exception("Insufficient balance");
            }

            user.Balance.AvailableBalance -= requiredMargin;
            order.Price = currentPrice;
            order.InitialCost = positionValue;
            order.Margin = requiredMargin;
        }

        await context.SaveChangesAsync();
    }
    /// <summary>
    /// Gets the bid price for a specific symbol.
    /// </summary>
    /// <param name="symbol">The symbol name.</param>
    /// <returns>The bid price.</returns>
    public async Task<float> GetBidPriceForSymbol(string symbol)
    {
        using var context = _contextFactory.CreateDbContext();
        var stock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == symbol);

        if (stock == null)
        {
            throw new Exception($"Stock {symbol} not found");
        }

        return stock.Bid;
    }

    /// <summary>
    /// Opens a pending order.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task OpenPendingOrder(Guid orderId, Guid userId)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = await context.Orders.Include(o => o.User).Include(u=> u.Stock).FirstOrDefaultAsync(o => o.Id == orderId);
        var user = await context.Users.Include(u => u.Orders).Include(u => u.Balance).FirstOrDefaultAsync(u => u.Id == userId);
        var stock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == order!.Stock.Name);

        if (order == null || user == null || stock == null)
        {
            throw new Exception("Order, User, or Stock not found");
        }

        if (order.OrderState != OrderState.Pending)
        {
            throw new Exception("Order is not in pending state");
        }

        var currentPrice = order.OrderType switch
        {
            OrderType.Buy => stock.Ask,
            OrderType.Short => stock.Bid,
            OrderType.BuyLimit => stock.Ask,
            OrderType.BuyStop => stock.Ask,
            OrderType.SellLimit => stock.Bid,
            OrderType.SellStop => stock.Bid,
            _ => throw new ArgumentOutOfRangeException()
        };

        float conversionRate = 1;
        if (stock.StockType == StockType.Forex)
        {
            conversionRate = await GetConversionRate(stock.Name, stock.StockType);
        }

        float positionValue = 0;
        float leverage = stock.Leverage;

        switch (stock.StockType)
        {
            case StockType.Forex:
                positionValue = order.QuantityInLots * 100000 * currentPrice * conversionRate;
                break;
            case StockType.Crypto:
                positionValue = order.QuantityInLots * currentPrice;
                break;
            case StockType.Commodities:
                positionValue = order.QuantityInLots * currentPrice;
                break;
            default:
                throw new Exception("Unknown Stock Type");
        }

        var requiredMargin = positionValue / leverage;

        if (user.Balance.AvailableBalance < requiredMargin)
        {
            throw new Exception("Insufficient balance");
        }

        user.Balance.AvailableBalance -= requiredMargin;
        order.OrderState = OrderState.Opened;
        order.InitialCost = positionValue;
        order.Price = currentPrice;
        order.Margin = requiredMargin;
        order.OrderDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }


    /// <summary>
    /// Executes an order.
    /// </summary>
    /// <param name="order">The order to execute.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteOrder(Order order, Guid userId)
    {
        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.Balance)
                    .FirstOrDefaultAsync(u => u.Id == userId);

        var currentStock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == order.Stock.Name);
        if (currentStock == null)
        {
            throw new Exception("Stock not found");
        }

        float conversionRate = 1;

        if (currentStock.StockType == StockType.Forex)
        {
                conversionRate = await GetConversionRate(currentStock.Name, currentStock.StockType);
        }


        if (user != null)
        {
            var currentPrice = order.OrderType switch
            {
                OrderType.Buy => currentStock.Bid,
                OrderType.Short => currentStock.Ask,
                OrderType.BuyLimit => currentStock.Bid,
                OrderType.BuyStop => currentStock.Bid,
                OrderType.SellLimit => currentStock.Ask,
                OrderType.SellStop => currentStock.Ask,
                _ => throw new ArgumentOutOfRangeException()
            };
            float marginRequired = 0;
            float initialCost = 0;
            float currentValue = 0;

            switch (currentStock.StockType)
            {
                case StockType.Forex:
                    marginRequired = (order.QuantityInLots * 100000 * order.Price) / 100; // Margin calculation
                    initialCost = order.QuantityInLots * 100000 * order.Price;
                    currentValue = order.QuantityInLots * 100000 * currentPrice;
                    break;
                case StockType.Crypto:
                    marginRequired = (order.QuantityInLots * order.Price) / 2; // Margin calculation
                    initialCost = order.QuantityInLots * order.Price;
                    currentValue = order.QuantityInLots * currentPrice;
                    break;
                case StockType.Commodities:
                    marginRequired = (order.QuantityInLots * order.Price) / 20; // Margin calculation
                    initialCost = order.QuantityInLots * order.Price;
                    currentValue = order.QuantityInLots * currentPrice;
                    break;
                default:
                    throw new Exception("Unknown Stock Type");
            }

            var latestOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
            if (latestOrder == null)
            {
                throw new Exception("Order not found");
            }
            float profitOrLoss = 0;

            if (order.OrderState == OrderState.Opened)
            {
                if (order.OrderType == OrderType.Buy || order.OrderType == OrderType.BuyLimit || order.OrderType == OrderType.BuyStop)
                {
                    profitOrLoss = currentValue - initialCost;
                }
                else if (order.OrderType == OrderType.Short || order.OrderType == OrderType.SellLimit || order.OrderType == OrderType.SellStop)
                {
                    profitOrLoss = initialCost - currentValue;
                }

                // Update user's balance
                user.Balance.AvailableBalance += marginRequired + profitOrLoss - latestOrder.ProfitOrLoss;
                user.Balance.TotalBalance += profitOrLoss;

                // Update the order state and closed price
                order.OrderState = OrderState.Completed;
                order.ClosedPrice = currentPrice;
                order.ProfitOrLoss = profitOrLoss;
            }
        }
        await context.SaveChangesAsync();
    }


    /// <summary>
    /// Floors a DateTime to the nearest minute.
    /// </summary>
    /// <param name="time">The DateTime to floor.</param>
    /// <returns>The floored DateTime.</returns>
    private DateTime FloorToNearestMinute(DateTime time)
    {
        return TimeHelper.FloorToNearestMinute(time);
    }

    /// <summary>
    /// Calculates the start date for a specific interval.
    /// </summary>
    /// <param name="interval">The interval string.</param>
    /// <returns>The calculated start date.</returns>
    private DateTime CalculateStartDate(string interval)
    {
        return TimeHelper.CalculateStartDate(interval);
    }

    /// <summary>
    /// Gets the current Eastern Time.
    /// </summary>
    /// <returns>The current Eastern Time.</returns>
    private DateTime GetEasternTime()
    {
        return TimeHelper.GetEasternTime();

    }


    /// <summary>
    /// Gets the time until the next interval.
    /// </summary>
    /// <param name="intervalMinutes">The interval duration in minutes.</param>
    /// <returns>The time until the next interval in milliseconds.</returns>
    private double GetTimeUntilNextInterval(int intervalMinutes)
    {
        return TimeHelper.GetTimeUntilNextInterval(intervalMinutes);

    }






    //private async Task AggregateHigherIntervalData(string symbol, string interval)
    //{
    //    using var context = _contextFactory.CreateDbContext();

    //    int intervalMinutes = interval switch
    //    {
    //        "5min" => 5,
    //        "15min" => 15,
    //        "30min" => 30,
    //        "1h" => 60,
    //        _ => throw new ArgumentException("Unsupported interval")
    //    };

    //    var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    //    var currentEasternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternTimeZone);
    //    var flooredEasternTime = FloorToNearestInterval(currentEasternTime, interval);
    //    DateTime startTime = flooredEasternTime.AddMinutes(-intervalMinutes);

    //    var recentData = await context.StockData
    //        .Where(sd => sd.Stock.Name == symbol && sd.Interval == "1min" && sd.TimeStamp >= startTime)
    //        .ToListAsync();


    //    if (recentData.Any())
    //    {
    //        var open = recentData.First().Open;
    //        var close = recentData.Last().Close;
    //        var high = recentData.Max(p => p.High);
    //        var low = recentData.Min(p => p.Low);
    //        var timestamp = FloorToNearestInterval(recentData.First().TimeStamp, interval);

    //        Stock stock = await context.Stocks.FirstOrDefaultAsync(s => s.Name == symbol);

    //        var existingStockData = await context.StockData
    //            .FirstOrDefaultAsync(sd => sd.TimeStamp == timestamp && sd.Interval == interval && sd.Stock == stock);

    //        if (existingStockData != null)
    //        {
    //            var aggregatedData = new StockData
    //            {
    //                TimeStamp = timestamp,
    //                Open = open,
    //                High = high,
    //                Low = low,
    //                Close = close,
    //                Interval = interval,
    //                Stock = stock
    //            };

    //            context.StockData.Add(aggregatedData);.
    //            await context.SaveChangesAsync();
    //        }


    //    }
    //}
}