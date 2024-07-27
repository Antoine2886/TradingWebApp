using Bd.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Utilities.BackgroundTask
{

    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: This service performs a weekly cleanup of old stock data to maintain a manageable amount of records in the database.
    /// </summary>

    public class WeeklyDataCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public WeeklyDataCleanupService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Executes the background service.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the service.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var scheduledDay = DayOfWeek.Sunday; // Adjust as needed
                var daysUntilScheduledDay = ((int)scheduledDay - (int)now.DayOfWeek + 7) % 7;
                var scheduledTime = now.AddDays(daysUntilScheduledDay).Date.AddHours(22); // 22:00 UTC
                var delay = scheduledTime > now ? scheduledTime - now : scheduledTime.AddDays(7) - now;

                await Task.Delay(delay, stoppingToken); // Wait until the scheduled time

                await CleanupOldData(stoppingToken, new List<string> { "15min", "30min", "45min", "1h" });
            }
        }


        /// <summary>
        /// Cleans up old stock data.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the operation.</param>
        /// <param name="intervals">List of intervals to clean up.</param>
        private async Task CleanupOldData(CancellationToken stoppingToken, List<string> intervals)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Context>();

                foreach (var interval in intervals)
                {
                    var symbols = await context.Stocks.Select(s => s.Name).ToListAsync();

                    foreach (var symbol in symbols)
                    {
                        if (stoppingToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var stockDataCount = await context.StockData
                            .Where(sd => sd.Stock.Name == symbol && sd.Interval == interval)
                            .CountAsync();

                        if (stockDataCount > 5000)
                        {
                            var recordsToDelete = await context.StockData
                                .Where(sd => sd.Stock.Name == symbol && sd.Interval == interval)
                                .OrderBy(sd => sd.TimeStamp)
                                .Take(stockDataCount - 5000)
                                .ToListAsync();

                            foreach (var batch in recordsToDelete.Batch(1000))
                            {
                                context.StockData.RemoveRange(batch);
                                await context.SaveChangesAsync();
                                await Task.Delay(1000, stoppingToken); // Introduce a delay to lower priority
                            }
                        }
                    }
                }
            }
        }
    }

}
