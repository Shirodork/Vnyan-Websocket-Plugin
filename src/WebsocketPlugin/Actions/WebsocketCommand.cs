namespace Logitech.LogiActions.WebsocketPlugin.Actions
{
    using System;

    public sealed class WebsocketCommand : PluginDynamicCommand
    {
        public WebsocketCommand()
            : base("VNyan Command", "Send a custom VNyan websocket message", "VNyan")
        {
            this.MakeProfileAction("text;Enter Websocket Command");
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Plugin is WebsocketPlugin plugin)
            {
                plugin.QueueSend(actionParameter);
            }
            else
            {
                PluginLog.Warning("Unable to resolve WebsocketPlugin instance for the command execution.");
            }
        }
    }
}
