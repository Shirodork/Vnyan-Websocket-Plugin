namespace Logitech.LogiActions.WebsocketPlugin
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Logitech.LogiActions.WebsocketPlugin.Helpers;

    public sealed class WebsocketPlugin : Plugin
    {
        private const String HostSetting = "Connection.Host";
        private const String PortSetting = "Connection.Port";
        private const String PathSetting = "Connection.Path";
        private const String UseTlsSetting = "Connection.UseTls";
        private const String ConnectTimeoutSetting = "Connection.ConnectTimeoutSeconds";
        private const String SendTimeoutSetting = "Connection.SendTimeoutSeconds";
        private const String KeepAliveSetting = "Connection.KeepAliveSeconds";
        private const String RetryCountSetting = "Connection.RetryCount";
        private const String RetryBaseDelaySetting = "Connection.RetryBaseDelayMilliseconds";
        private const String RetryMaxDelaySetting = "Connection.RetryMaxDelayMilliseconds";

        private WebSocketConnectionManager _connectionManager;

        public override Boolean UsesApplicationApiOnly => true;

        public override Boolean HasNoApplication => true;

        public WebsocketPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
            _connectionManager = new WebSocketConnectionManager(CreateDefaultOptions());
        }

        public override void Load()
        {
            RefreshConnectionOptions();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _connectionManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "Warm-up websocket connection attempt failed.");
                }
            });
        }

        public override void Unload()
        {
            _connectionManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        internal void QueueSend(String? payload)
        {
            if (String.IsNullOrWhiteSpace(payload))
            {
                PluginLog.Warning("Ignoring websocket send request without a payload.");
                return;
            }

            var trimmed = payload.Trim();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _connectionManager.SendMessageAsync(trimmed, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed to send websocket command '{trimmed}'.");
                }
            });
        }

        internal void RefreshConnectionOptions()
        {
            var options = BuildOptions();
            _connectionManager.UpdateOptions(options);
        }

        private WebSocketConnectionOptions BuildOptions()
        {
            var host = GetOrCreateSetting(HostSetting, "127.0.0.1");
            var port = GetOrCreateInt32(PortSetting, 8000, 1, 65535);
            var path = GetOrCreateSetting(PathSetting, "vnyan");
            var useTls = GetOrCreateBoolean(UseTlsSetting, false);
            var connectTimeoutSeconds = GetOrCreateInt32(ConnectTimeoutSetting, 5, 1, 120);
            var sendTimeoutSeconds = GetOrCreateInt32(SendTimeoutSetting, 5, 1, 120);
            var keepAliveSeconds = GetOrCreateInt32(KeepAliveSetting, 30, 0, 600);
            var retryCount = GetOrCreateInt32(RetryCountSetting, 3, 1, 10);
            var retryBaseDelay = GetOrCreateInt32(RetryBaseDelaySetting, 200, 50, 10000);
            var retryMaxDelay = GetOrCreateInt32(RetryMaxDelaySetting, 2000, 100, 60000);

            var builder = new UriBuilder
            {
                Scheme = useTls ? "wss" : "ws",
                Host = host,
                Port = port,
                Path = SanitizePath(path)
            };

            return new WebSocketConnectionOptions(
                builder.Uri,
                TimeSpan.FromSeconds(connectTimeoutSeconds),
                TimeSpan.FromSeconds(sendTimeoutSeconds),
                TimeSpan.FromSeconds(keepAliveSeconds),
                retryCount,
                TimeSpan.FromMilliseconds(retryBaseDelay),
                TimeSpan.FromMilliseconds(Math.Max(retryBaseDelay, retryMaxDelay)));
        }

        private String GetOrCreateSetting(String key, String defaultValue)
        {
            if (this.TryGetPluginSetting(key, out var value) && !String.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            this.SetPluginSetting(key, defaultValue, false);
            return defaultValue;
        }

        private Int32 GetOrCreateInt32(String key, Int32 defaultValue, Int32 min, Int32 max)
        {
            var text = GetOrCreateSetting(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return Math.Clamp(parsed, min, max);
            }

            this.SetPluginSetting(key, defaultValue.ToString(CultureInfo.InvariantCulture), false);
            return defaultValue;
        }

        private Boolean GetOrCreateBoolean(String key, Boolean defaultValue)
        {
            var text = GetOrCreateSetting(key, defaultValue ? Boolean.TrueString : Boolean.FalseString);
            if (Boolean.TryParse(text, out var parsed))
            {
                return parsed;
            }

            this.SetPluginSetting(key, defaultValue ? Boolean.TrueString : Boolean.FalseString, false);
            return defaultValue;
        }

        private static String SanitizePath(String path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            var trimmed = path.Trim();
            return trimmed.Trim('/');
        }

        private static WebSocketConnectionOptions CreateDefaultOptions()
            => new(
                new Uri("ws://127.0.0.1:8000/vnyan"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30),
                3,
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(2000));
    }
}
