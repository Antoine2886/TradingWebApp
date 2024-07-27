using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using WebApp.Utilities.Trading;

namespace WebApp.Utilities.BackgroundTask
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Background service to manage the lifecycle of the TradeService's WebSocket connection.
    /// </summary>
    public class TradeServiceBackgroundService : IHostedService
    {
        private readonly ITradeService _tradeService;
        private Task _webSocketTask;
        private CancellationTokenSource _cancellationTokenSource;

        public TradeServiceBackgroundService(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        /// <summary>
        /// Starts the background service and initiates the WebSocket connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the service.</param>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _webSocketTask = Task.Run(() => _tradeService.ConnectWebSocket(), _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the background service and terminates the WebSocket connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the service.</param>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_webSocketTask != null)
            {
                _cancellationTokenSource.Cancel();
                return Task.WhenAny(_webSocketTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            return Task.CompletedTask;
        }
    }
}
