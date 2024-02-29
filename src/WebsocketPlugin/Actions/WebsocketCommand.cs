namespace Loupedeck.WebsocketPlugin.Actions
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Loupedeck.WebsocketPlugin.Helpers;

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
