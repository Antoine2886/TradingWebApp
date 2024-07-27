using Bd.Enums;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp.Utilities.Token;
using WebApp.Utilities.Trading;
using WebApp.Utilities.Trading.DTO;
using System;
using System.Text.Json.Serialization;
using Stripe.Climate;
using Microsoft.AspNetCore.SignalR;
using WebApp.Utilities.Trading.TradingLobby;
using Sprache;
namespace WebApp.Controllers
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Controller to handle trading operations including placing orders, retrieving historical data, and managing user settings.
    /// </summary
    [ApiController]
    [Route("api/[controller]")]
    public class TradeController : Controller
    {
        private readonly IDbContextFactory<Context> _contextFactory;
        private readonly ITokenRepository _tokenRepository;
        private readonly ITradeService _tradeService;
        private readonly IHubContext<TradeHub> _hubContext;
        public TradeController(IDbContextFactory<Context> contextFactory, ITokenRepository tokenRepository, ITradeService tradeService, IHubContext<TradeHub> hubContext)
        {
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            _tokenRepository = tokenRepository;
            _tradeService = tradeService;
        }

        /// <summary>
        /// Gets the list of available trading symbols.
        /// </summary>
        [HttpGet("symbols")]
        public async Task<IActionResult> GetSymbols()
        {
            using var context = _contextFactory.CreateDbContext();
            var symbols = await context.Stocks.Select(s => s.Name).ToListAsync();
            return Ok(symbols);
        }
        //[HttpGet]
        //public async Task<IActionResult> Trade(Guid chartSettingsId)
        //{
        //    using var _context = _contextFactory.CreateDbContext();

        //    var chartSettings = await _context.ChartSettings.FindAsync(chartSettingsId);

        //    if (chartSettings == null)
        //    {
        //        return NotFound();
        //    }

        //    TradeViewModel model = new TradeViewModel
        //    {
        //        Symbol = chartSettings.Symbol,
        //        Interval = chartSettings.Interval,
        //        ChartType = chartSettings.ChartType,
        //        TimeZone = chartSettings.TimeZone,
        //        Theme = chartSettings.Theme,
        //        LineColor = chartSettings.LineColor,
        //        UpColor = chartSettings.UpColor,
        //        DownColor = chartSettings.DownColor,
        //        Drawings = chartSettings.Drawings
        //    };
        //    ViewData["ChartSettingsId"] = chartSettingsId;
        //    return View(model);
        //}

        /// <summary>
        /// Gets historical data for a given symbol and interval.
        /// </summary>
        [HttpGet("historical")]
        public async Task<IActionResult> GetHistoricalData(string symbol, string interval)
        {
            using var context = _contextFactory.CreateDbContext();

            var totalRecords = await context.StockData
                .Where(sd => sd.Stock.Name == symbol && sd.Interval == interval)
                .CountAsync();

            var data = await context.StockData
                .Where(sd => sd.Stock.Name == symbol && sd.Interval == interval)
                .OrderByDescending(sd => sd.TimeStamp) // Order by TimeStamp descending to get the most recent data
                .Take(5000) // Limit to the first 5000 records
                .OrderBy(sd => sd.TimeStamp) // Order again ascending to return data in correct order
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Renders the Index view and provides a token for the user.
        /// </summary>
        [HttpGet("Index")]

        public IActionResult Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;
            var token = _tokenRepository.CreateTokenAsync(userId).Result;
            ViewBag.Token = token;
            ViewBag.UserId = userId;
            return View();
        }

        /// <summary>
        /// Gets the latest data for a given symbol and interval.
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(string symbol, string interval)
        {
            var latestData = await _tradeService.GetLatestData(symbol, interval);
            if (latestData == null)
            {
                return NotFound();
            }
            return Ok(latestData);
        }


        /// <summary>
        /// Gets the bid and ask prices for a given symbol.
        /// </summary>
        [HttpGet("bid-ask")]
        public async Task<IActionResult> GetBidAsk(string symbol)
        {
            var bidAsk = await _tradeService.GetBidAskData(symbol);
            if (bidAsk == null)
            {
                return NotFound();
            }
            return Ok(bidAsk);
        }
        /// <summary>
        /// Gets the balance for the logged-in user.
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            var balanceData = await _tradeService.GetBalanceData(userId);
            if (balanceData == null)
            {
                return NotFound(new { success = false, message = "Balance data not found." });
            }

            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveBalanceUpdate", balanceData);

            return Ok(new { success = true, data = balanceData });
        }
        /// <summary>
        /// Gets the orders for the logged-in user.
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized("User not logged in.");
                }

                var userId = Guid.Parse(userIdString);

                var orderData = await _tradeService.GetOrderData(userId);
                if (orderData == null || !orderData.Any())
                {
                    return NotFound(new { success = false, message = "No orders found." });
                }

                return Ok(new { success = true, data = orderData });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) if necessary
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        /// <summary>
        /// Places a new order for the logged-in user.
        /// </summary>
        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest orderRequest)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized("User not logged in.");
                }

                var userId = Guid.Parse(userIdString);

                // Check if the lot size is greater than 100
                if (orderRequest.QuantityInLots > 100f || orderRequest.QuantityInLots < 0.01f)
                {
                    return BadRequest(new { success = false, message = "Lot size cannot be greater than 100 or smaller than 0.01." });
                }

                // Check if the take profit or stop loss is negative
                if (orderRequest.StopLoss < 0 || orderRequest.TakeProfit < 0)
                {
                    return BadRequest(new { success = false, message = "Stop loss and take profit cannot be negative." });
                }

                // Check if the symbol exists in the database
                var symbolExists = await context.Stocks.AnyAsync(s => s.Name == orderRequest.Symbol);
                if (!symbolExists)
                {
                    return BadRequest(new { success = false, message = "Symbol does not exist." });
                }
                var stock = await context.Stocks.Where(s => s.Name == orderRequest.Symbol).FirstOrDefaultAsync();

                if (stock.Bid == 0 || stock.Ask == 0) { 
                
                    return BadRequest(new { success = false, message = "No bid or ask" });

                }



                await _tradeService.PlaceOrder(userId, orderRequest.Symbol, orderRequest.OrderType, orderRequest.QuantityInLots, orderRequest.Price, orderRequest.StopLoss, orderRequest.TakeProfit);
                await context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order placed successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) if necessary
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        /// <summary>
        /// Executes an order for the logged-in user.
        /// </summary>
        [HttpPost("execute-order")]
        public async Task<IActionResult> ExecuteOrder([FromBody] ExecuteOrderRequest request)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized("User not logged in.");
                }

                var userId = Guid.Parse(userIdString);

                using var context = _contextFactory.CreateDbContext();
                // Fetch the order from the database based on the order ID
                var orderFromDb = await context.Orders.Include(o => o.User).Include(o => o.Stock).FirstOrDefaultAsync(o => o.Id == request.Id);

                // Validate order
                if (orderFromDb == null || orderFromDb.User.Id != userId)
                {
                    return BadRequest("Invalid order.");

                }

                // Check if the order is already completed or pending
                if (orderFromDb.OrderState == OrderState.Completed || orderFromDb.OrderState == OrderState.Pending)
                {
                    return BadRequest(new { success = false, message = "Order cannot be executed because it is either already completed or pending." });
                }

                await _tradeService.ExecuteOrder(orderFromDb, userId);
                await context.SaveChangesAsync();
                return Ok(new { success = true, message = "Order executed successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) if necessary
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        /// <summary>
        /// Saves chart settings for the logged-in user.
        /// </summary>
        [HttpPost("save-settings")]
        public async Task<IActionResult> SaveSettings()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            string settingsJson = Request.Form["Settings"];
            string saveNewString = Request.Form["SaveNew"];

            var settings = ParseChartSettings(settingsJson);
            settings.UserId = userId.ToString();

            bool saveNew = bool.Parse(saveNewString);

            using var context = _contextFactory.CreateDbContext();
            var existingSettings = await context.ChartSettings
                .Include(cs => cs.Drawings)
                .FirstOrDefaultAsync(s => s.UserId == settings.UserId && s.Symbol == settings.Symbol && s.Id == settings.Id);

            if (saveNew)
            {
                var userChartCount = await context.ChartSettings
                    .CountAsync(s => s.UserId == settings.UserId);

                if (userChartCount >= 3)
                {
                    return BadRequest("A maximum of 3 charts is allowed per user.");
                }

                if (settings.Drawings.Count > 10)
                {
                    return BadRequest("A maximum of 10 drawings is allowed.");
                }

                if (settings.Id == Guid.Empty)
                {
                    settings.Id = Guid.NewGuid();
                }

                context.ChartSettings.Add(settings);
            }
            else
            {
                if (existingSettings == null)
                {
                    if (settings.Drawings.Count > 10)
                    {
                        return BadRequest("A maximum of 10 drawings is allowed.");
                    }
                    context.ChartSettings.Add(settings);
                }
                else
                {
                    if (settings.Drawings.Count > 10)
                    {
                        return BadRequest("A maximum of 10 drawings is allowed.");
                    }

                    // Retain the existing ID for the settings
                    settings.Id = existingSettings.Id;

                    context.Entry(existingSettings).CurrentValues.SetValues(settings);
                    existingSettings.Drawings.Clear();
                    existingSettings.Drawings.AddRange(settings.Drawings);
                }
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Saves new chart settings for the logged-in user.
        /// </summary>
        [HttpPost("save-new-settings")]
        public async Task<IActionResult> SaveNewSettings()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            string settingsJson = Request.Form["Settings"];

            var settings = ParseChartSettings(settingsJson);
            settings.UserId = userId.ToString();
            if (settings.Id == Guid.Empty)
            {
                settings.Id = Guid.NewGuid();
            }

            using var context = _contextFactory.CreateDbContext();
            var userChartCount = await context.ChartSettings.CountAsync(s => s.UserId == settings.UserId);

            if (userChartCount >= 3)
            {
                return BadRequest("A maximum of 3 charts is allowed per user.");
            }

            if (settings.Drawings.Count > 10)
            {
                return BadRequest("A maximum of 10 drawings is allowed.");
            }

            context.ChartSettings.Add(settings);
            await context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Loads chart settings for the logged-in user.
        /// </summary>
        [HttpGet("load-settings")]
        public async Task<IActionResult> LoadSettings(Guid? id = null)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            using var context = _contextFactory.CreateDbContext();
            var query = context.ChartSettings.Include(cs => cs.Drawings).Where(s => s.UserId == userId.ToString());
            

            var settings = id.HasValue
                ? await query.Where(s => s.Id == id.Value).Select(s => new
                {
                    s.Id,
                    s.Symbol,
                    s.Interval,
                    s.ChartType,
                    s.TimeZone,
                    s.Theme,
                    s.LineColor,
                    s.UpColor,
                    s.DownColor,
                    Drawings = s.Drawings.Select(d => new { d.Type, d.Coordinates }).ToList()
                }).FirstOrDefaultAsync()
                : await query.Select(s => new
                {
                    s.Id,
                    s.Symbol,
                    s.Interval,
                    s.ChartType,
                    s.TimeZone,
                    s.Theme,
                    s.LineColor,
                    s.UpColor,
                    s.DownColor,
                    Drawings = s.Drawings.Select(d => new { d.Type, d.Coordinates }).ToList()
                }).FirstOrDefaultAsync();

            if (settings == null)
            {
                return NotFound();
            }

            return Ok(settings);
        }

        /// <summary>
        /// Loads chart settings by ID.
        /// </summary>
        [HttpGet("load-settings-by-id")]
        public async Task<IActionResult> LoadSettingsById(Guid id)
        {
            using var context = _contextFactory.CreateDbContext();

            var settings = await context.ChartSettings.Include(cs => cs.Drawings)
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Symbol,
                    s.Interval,
                    s.ChartType,
                    s.TimeZone,
                    s.Theme,
                    s.LineColor,
                    s.UpColor,
                    s.DownColor,
                    Drawings = s.Drawings.Select(d => new { d.Type, d.Coordinates }).ToList()
                })
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                return NotFound();
            }

            return Ok(settings);
        }


        /// <summary>
        /// Gets all chart settings for the logged-in user.
        /// </summary>

        [HttpGet("all-settings")]
        public async Task<IActionResult> GetAllSettings()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            using var context = _contextFactory.CreateDbContext();
            var settings = await context.ChartSettings
                .Where(s => s.UserId == userId.ToString())
                .Include(cs => cs.Drawings)
                .ToListAsync();

            return Ok(settings);
        }


        public class SaveChartSettingsRequest
        {
            public ChartSettings Settings { get; set; }
            public bool SaveNew { get; set; }
        }


        /// <summary>
        /// Parses the JSON string to create a ChartSettings object.
        /// </summary>
        /// <param name="json">The JSON string containing chart settings.</param>
        /// <returns>The parsed ChartSettings object.</returns>
        private ChartSettings ParseChartSettings(string json)
        {
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            var settings = new ChartSettings
            {
                Id = root.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.GetString(), out var id) ? id : Guid.Empty,
                Symbol = root.GetProperty("symbol").GetString(),
                Interval = root.GetProperty("interval").GetString(),
                ChartType = root.GetProperty("chartType").GetString(),
                TimeZone = root.GetProperty("timeZone").GetString(),
                Theme = root.GetProperty("theme").GetString(),
                LineColor = root.GetProperty("lineColor").GetString(),
                UpColor = root.GetProperty("upColor").GetString(),
                DownColor = root.GetProperty("downColor").GetString(),
                Drawings = root.GetProperty("drawings").EnumerateArray()
                    .Select(d => new Drawing
                    {
                        Type = d.GetProperty("type").GetString(),
                        Coordinates = d.GetProperty("coordinates").GetString()
                    })
                    .ToList()
            };

            return settings;
        }










        public class ExecuteOrderRequest
        {
            public Guid Id { get; set; }
        }

        public class OrderRequest
        {
            public string Symbol { get; set; }

            [JsonConverter(typeof(OrderTypeConverter))]
            public OrderType OrderType { get; set; }

            public float QuantityInLots { get; set; }
            public float Price { get; set; }
            public float? StopLoss { get; set; }
            public float? TakeProfit { get; set; }
        }
        public class OrderTypeConverter : JsonConverter<OrderType>
        {
            public override OrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var value = reader.GetString();
                return Enum.TryParse<OrderType>(value, true, out var result) ? result : throw new JsonException($"Invalid value for OrderType: {value}");
            }

            public override void Write(Utf8JsonWriter writer, OrderType value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
