using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Author: Antoine Bélanger
/// Description: Implements a rate limiter that restricts the number of allowed requests per minute.
/// </summary>
public class RateLimiter
{
    private readonly int _maxRequestsPerMinute;
    private readonly ConcurrentQueue<DateTime> _requestTimestamps;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Initializes a new instance of the RateLimiter class.
    /// </summary>
    /// <param name="maxRequestsPerMinute">The maximum number of requests allowed per minute.</param>
    public RateLimiter(int maxRequestsPerMinute)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute;
        _requestTimestamps = new ConcurrentQueue<DateTime>();
        _semaphore = new SemaphoreSlim(maxRequestsPerMinute, maxRequestsPerMinute);
    }

    /// <summary>
    /// Determines if a request is allowed based on the rate limiting rules.
    /// </summary>
    /// <returns>A task that represents the completion of the request check. The task result contains true if the request is allowed; otherwise, false.</returns>
    public async Task<bool> AllowRequestAsync()
    {
        await _semaphore.WaitAsync();

        lock (_requestTimestamps)
        {
            // Remove timestamps older than 1 minute
            while (_requestTimestamps.TryPeek(out var timestamp) && timestamp <= DateTime.UtcNow.AddMinutes(-1))
            {
                _requestTimestamps.TryDequeue(out _);
            }

            if (_requestTimestamps.Count < _maxRequestsPerMinute)
            {
                _requestTimestamps.Enqueue(DateTime.UtcNow);
                _semaphore.Release();
                return true;
            }
            else
            {
                // Schedule the next request after the oldest one falls out of the 1 minute window
                if (_requestTimestamps.TryPeek(out var firstTimestamp))
                {
                    var delay = DateTime.UtcNow.AddMinutes(1) - firstTimestamp;
                    _ = Task.Delay(delay).ContinueWith(t => _semaphore.Release());
                }
                return false;
            }
        }
    }
}
