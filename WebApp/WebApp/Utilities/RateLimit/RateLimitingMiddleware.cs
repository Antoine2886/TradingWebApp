using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTimes = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();
    private readonly TimeSpan _blockDuration = TimeSpan.FromSeconds(10);
    private readonly int _defaultMaxRequests = 500;
    private readonly TimeSpan _defaultTimeWindow = TimeSpan.FromSeconds(10);


    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Middleware to implement rate limiting based on client IP and request timestamps.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to check and enforce rate limiting.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(clientIp))
        {
            await _next(context);
            return;
        }

        var currentTime = DateTime.UtcNow;
        var requestQueue = _requestTimes.GetOrAdd(clientIp, new ConcurrentQueue<DateTime>());

        // Define custom limits for specific endpoints
        var endpoint = context.Request.Path.ToString().ToLower();
        var (maxRequests, timeWindow) = GetRateLimitsForEndpoint(endpoint);

        // Clean up old request timestamps
        while (requestQueue.TryPeek(out var timestamp) && (currentTime - timestamp) > timeWindow)
        {
            requestQueue.TryDequeue(out _);
        }

        if (requestQueue.Count >= maxRequests)
        {
            // Check if the last request was within the block duration
            var lastRequestTime = requestQueue.Last();
            if ((currentTime - lastRequestTime) < _blockDuration)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Try again later.");
                return;
            }
        }

        // Add the current request timestamp
        requestQueue.Enqueue(currentTime);
        await _next(context);
    }

    /// <summary>
    /// Gets the rate limits for a specific endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to get rate limits for.</param>
    /// <returns>A tuple containing the max requests and time window for the endpoint.</returns>
    private (int maxRequests, TimeSpan timeWindow) GetRateLimitsForEndpoint(string endpoint)
    {
        // Customize limits for specific endpoints
        return endpoint switch
        {
            "/api/trade/balance" => (1, TimeSpan.FromSeconds(30)),
            _ => (_defaultMaxRequests, _defaultTimeWindow),
        };
    }
}
