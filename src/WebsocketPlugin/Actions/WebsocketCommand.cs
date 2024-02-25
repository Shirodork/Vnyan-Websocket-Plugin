namespace Loupedeck.WebsocketPlugin.Actions
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Loupedeck.WebsocketPlugin.Helpers;

    /*    public class WebsocketCommand : PluginDynamicCommand
        {

            public WebsocketCommand()
                : base("Vnyan Websocket Connection", "Send a Websocket Message to Vnyan. To use, use the Callback Websocket Node on Vnyan and match the message to trigger Vnyan responses", "Websockets") 
            {
                this.MakeProfileAction("text;Enter Websocket Command"); // Allow Message Input
            }

            protected override void RunCommand(String actionParameter)
            {
                Task.Run(() => SendWebSocketMessageAsync(actionParameter));
            }

            private async Task SendWebSocketMessageAsync(String actionParameter)
            {
                using (var webSocket = new ClientWebSocket())
                {
                    Uri serverUri = new Uri("ws://localhost:8000/vnyan");

                    await webSocket.ConnectAsync(serverUri, CancellationToken.None);

                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(actionParameter));

                    await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

                }
            }
        }*/
    public class WebsocketCommand : PluginDynamicCommand
    {
        private static WebSocketConnectionManager connectionManager = new WebSocketConnectionManager();

        public WebsocketCommand() : base("Websocket Connection", "Send a Websocket Message", "Websockets")
        {
            this.MakeProfileAction("text;Enter Websocket Command");
        }

        protected override void RunCommand(String actionParameter)
        {
            // Asynchronously send a message without awaiting here to avoid blocking the UI thread
            Task.Run(() => connectionManager.SendMessageAsync(actionParameter));
        }
    }

}
