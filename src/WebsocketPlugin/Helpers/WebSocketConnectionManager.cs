namespace Loupedeck.WebsocketPlugin.Helpers
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class WebSocketConnectionManager
    {
        // Set up some stuff
        private ClientWebSocket webSocket = null;
        private static readonly object lockObject = new object();
        private Timer inactivityTimer;

        public WebSocketConnectionManager()
        {
            // Initialize the timer but don't start it yet (null callback, dueTime: Infinite, period: Infinite)
            inactivityTimer = new Timer(_ => CloseWebSocketDueToInactivity(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task SendMessageAsync(string message)
        {
            lock (lockObject)
            {
                if (webSocket == null || webSocket.State != WebSocketState.Open)
                {
                    webSocket?.Dispose();
                    webSocket = new ClientWebSocket();
                    // Synchronously wait for the connection to establish to keep the lock
                    webSocket.ConnectAsync(new Uri("ws://localhost:8000/vnyan"), CancellationToken.None).Wait();
                }
            }

            inactivityTimer.Change(TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        private void CloseWebSocketDueToInactivity()
        {
            lock (lockObject)
            {
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing due to inactivity", CancellationToken.None).Wait();
                    webSocket.Dispose();
                    webSocket = null;
                }
            }
        }
    }
}
