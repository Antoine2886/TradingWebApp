using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, int> _requestCounts = new ConcurrentDictionary<string, int>();
    private static readonly string logFilePath = "requests.log"; // Path to the log file


    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Middleware to log HTTP requests, including client IP, request path, request count, and execution time.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger to log information and errors.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    /// <summary>
    /// Invokes the middleware to log the details of the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var requestPath = context.Request.Path.ToString();
        var stopwatch = Stopwatch.StartNew();

        if (!string.IsNullOrEmpty(clientIp))
        {
            _requestCounts.AddOrUpdate(clientIp, 1, (key, count) => count + 1);
        }

        await _next(context);

        stopwatch.Stop();
        var executionTime = stopwatch.ElapsedMilliseconds;

        if (!string.IsNullOrEmpty(clientIp))
        {
            var logMessage = $"Client IP: {clientIp}, Path: {requestPath}, Request Count: {_requestCounts[clientIp]}, Execution Time: {executionTime} ms";
            _logger.LogInformation(logMessage);
            LogToFile(logMessage);
        }
    }
    /// <summary>
    /// Logs a message to the log file.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void LogToFile(string message)
    {
        try
        {
            using (var writer = File.AppendText(logFilePath))
            {
                writer.WriteLine($"{DateTime.UtcNow}: {message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to write to log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the request count for a specific client IP.
    /// </summary>
    /// <param name="clientIp">The client IP to get the request count for.</param>
    /// <returns>The request count for the specified client IP.</returns>
    public static int GetRequestCount(string clientIp)
    {
        return _requestCounts.TryGetValue(clientIp, out var count) ? count : 0;
    }
}
