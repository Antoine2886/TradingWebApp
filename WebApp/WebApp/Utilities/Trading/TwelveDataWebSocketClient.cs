namespace WebApp.Utilities.Trading
{
    using Bd.Infrastructure;
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class TwelveDataWebSocketClient : IDisposable
    {
        private readonly ClientWebSocket _webSocket;
        private readonly string _apiKey;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;
        public bool IsConnected => _isConnected;

        public TwelveDataWebSocketClient(string apiKey)
        {
            _webSocket = new ClientWebSocket();
            _apiKey = apiKey;
            _cancellationTokenSource = new CancellationTokenSource();
            _isConnected = false;
        }

        /// <summary>
        /// Establishes a WebSocket connection to the Twelve Data WebSocket API.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task Connect()
        {
            Uri uri = new Uri($"wss://ws.twelvedata.com/v1/quotes/price?apikey={_apiKey}");
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            _isConnected = true;
        }

        /// <summary>
        /// Receives a message from the WebSocket connection.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation that returns the received message as a string.</returns>
        public async Task<string> ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

            // Check if the WebSocket connection has been closed
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                return null; // Return null to indicate that the connection has been closed
            }

            // Decode only the portion of the buffer that contains the received message
            return Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        }

        /// <summary>
        /// Sends a subscription request to the Twelve Data WebSocket API for the specified symbols.
        /// </summary>
        /// <param name="symbols">A string containing the symbols to subscribe to.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendSubscribeEvent(List<string> symbols)
        {
            string formattedSymbols = string.Join(",", symbols.Select(s => s.Replace(" ", "")));
            string subscribeEvent = $"{{\"action\": \"subscribe\", \"params\": {{\"symbols\": \"{formattedSymbols}\"}}}}";
            byte[] buffer = Encoding.UTF8.GetBytes(subscribeEvent);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Sends an unsubscription request to the Twelve Data WebSocket API for the specified symbols.
        /// </summary>
        /// <param name="symbols">A string containing the symbols to unsubscribe from.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendUnsubscribeEvent(string symbols)
        {
            string unsubscribeEvent = $"{{\"action\": \"unsubscribe\", \"params\": {{\"symbols\": \"{symbols}\"}}}}";
            byte[] buffer = Encoding.UTF8.GetBytes(unsubscribeEvent);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

        }
        /// <summary>
        /// Disposes of the WebSocket client, canceling any ongoing operations and releasing associated resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _webSocket.Dispose();
            _isConnected = false;
        }




    }



}
