using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Configuration;
using WebApp.Utilities.BackgroundTask;
using WebApp.Utilities.Email;
using WebApp.Utilities.Token;
using WebApp.Utilities.Trading;
using WebApp.Utilities.Trading.TradingLobby;
using DotNetEnv;
using WebApp.Utilities.Trading.Initialisation;
using Serilog;
using Stripe;
var builder = WebApplication.CreateBuilder(args);

Env.Load();

// Retrieve the API keys from environment variables
string twelveDataApiKey = Environment.GetEnvironmentVariable("TWELVE_DATA_API_KEY");
string twelveDataWsKey = Environment.GetEnvironmentVariable("TWELVE_DATA_WS_KEY");
// Override DefaultConnection from environment variables if set
string connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddControllers();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential 
    // cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;

    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.ConsentCookieValue = "true";
});


builder.Services.AddDbContextFactory<Context>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 3)));
});

builder.Services.AddSignalR();
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>().AddEntityFrameworkStores<Context>().AddDefaultTokenProviders();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITokenRepository, TokenUtility>();
builder.Services.AddSingleton<ITradeService, TradeService>();
builder.Services.AddHostedService<TradeServiceBackgroundService>();
builder.Services.AddHostedService<WeeklyDataCleanupService>();
builder.Services.AddHostedService<DailyDataCleanupService>();

builder.Services.AddSingleton(provider =>
{
    return new TwelveDataApiClient(twelveDataApiKey);
});
builder.Services.AddSingleton(provider =>
{
    return new TwelveDataWebSocketClient(twelveDataWsKey);
});




var app = builder.Build();

// Register observers
void RegisterObservers()
{
    var tradeService = app.Services.GetRequiredService<ITradeService>() as TradeService;


}

// Call the method to register observers
RegisterObservers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.MapHub<TradeHub>("/tradeHub");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<Context>();
    StockInitializer.InitializeStocks(context);
}
app.Run();
