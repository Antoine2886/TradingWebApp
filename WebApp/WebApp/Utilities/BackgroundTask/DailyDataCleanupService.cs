using Bd.Infrastructure;
using Microsoft.EntityFrameworkCore;



/// <summary>
/// Author: Antoine Bélanger
/// Description: This service performs a daily cleanup of old stock data to maintain a manageable amount of records in the database.
/// </summary>
public class DailyDataCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DailyDataCleanupService(IServiceScopeFactory serviceScopeFactory)
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
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0, DateTimeKind.Utc);
            var delay = scheduledTime > now ? scheduledTime - now : scheduledTime.AddDays(1) - now;

            await Task.Delay(delay, stoppingToken); // Wait until 22:00 UTC

            await CleanupOldData(stoppingToken, new List<string> { "1min", "5min" });
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

/// <summary>
/// Author: Antoine Bélanger
/// Description: Helper extension method to batch collections into smaller groups.
/// </summary>
public static class IEnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        T[] bucket = null;
        var count = 0;

        foreach (var item in source)
        {
            if (bucket == null)
            {
                bucket = new T[size];
            }

            bucket[count++] = item;

            if (count != size)
            {
                continue;
            }

            yield return bucket.Select(x => x);

            bucket = null;
            count = 0;
        }

        // Return the last bucket with all remaining items
        if (bucket != null && count > 0)
        {
            yield return bucket.Take(count);
        }
    }
}
