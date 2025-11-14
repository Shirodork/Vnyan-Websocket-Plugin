namespace Logitech.LogiActions.WebsocketPlugin
{
    using System;

    // This class can be used to connect the plugin to a foreground application if VNyan exposes one.

    public sealed class WebsocketApplication : ClientApplication
    {
        public WebsocketApplication()
        {
        }

        // This method can be used to link the plugin to a Windows application.
        protected override String GetProcessName() => "";

        // This method can be used to link the plugin to a macOS application.
        protected override String GetBundleName() => "";

        // This method can be used to check whether the application is installed or not.
        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
