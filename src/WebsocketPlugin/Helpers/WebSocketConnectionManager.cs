namespace Logitech.LogiActions.WebsocketPlugin.Helpers
{
    using System;
    using System.IO;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class WebSocketConnectionManager : IAsyncDisposable
    {
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private readonly SemaphoreSlim _sendGate = new(1, 1);

        private ClientWebSocket? _client;
        private Task? _keepAliveTask;
        private CancellationTokenSource? _keepAliveCts;
        private Boolean _disposed;
        private WebSocketConnectionOptions _options;

        public WebSocketConnectionManager(WebSocketConnectionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Initial websocket connection attempt failed. The plugin will retry on demand.");
            }
        }

        public void UpdateOptions(WebSocketConnectionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _ = ResetConnectionAsync();
        }

        public async Task SendMessageAsync(String message, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                PluginLog.Warning("Websocket command ignored because the payload was empty.");
                return;
            }

            EnsureNotDisposed();

            var trimmed = message.Trim();
            var attempts = 0;
            Exception? lastError = null;

            while (attempts < _options.MaxRetryCount && !cancellationToken.IsCancellationRequested)
            {
                attempts++;
                try
                {
                    var socket = await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                    await SendInternalAsync(socket, trimmed, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    lastError = ex;
                    PluginLog.Warning(ex, $"Websocket send attempt {attempts} failed. Retrying.");
                    await ResetConnectionAsync().ConfigureAwait(false);
                    if (attempts < _options.MaxRetryCount)
                    {
                        var delay = _options.GetRetryDelay(attempts);
                        try
                        {
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (lastError is not null)
            {
                throw lastError;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await ResetConnectionAsync().ConfigureAwait(false);
            _connectionGate.Dispose();
            _sendGate.Dispose();
        }

        private async Task<ClientWebSocket> EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            var existing = _client;
            if (existing is { State: WebSocketState.Open })
            {
                return existing;
            }

            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                existing = _client;
                if (existing is { State: WebSocketState.Open })
                {
                    return existing;
                }

                await ResetConnectionCoreAsync().ConfigureAwait(false);

                var client = CreateClient();

                using var timeoutCts = new CancellationTokenSource(_options.ConnectTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                try
                {
                    await client.ConnectAsync(_options.Endpoint, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Timed out after {_options.ConnectTimeout} while connecting to '{_options.Endpoint}'.", ex);
                }

                _client = client;
                _keepAliveCts = new CancellationTokenSource();
                _keepAliveTask = Task.Run(() => RunKeepAliveAsync(client, _keepAliveCts.Token));
                PluginLog.Info($"Connected to {_options.Endpoint}.");
                return client;
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        private async Task SendInternalAsync(ClientWebSocket socket, String payload, CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(_options.SendTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            await _sendGate.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                await socket.SendAsync(new ArraySegment<Byte>(bytes), WebSocketMessageType.Text, true, linkedCts.Token).ConfigureAwait(false);
                PluginLog.Verbose($"Sent websocket message: '{payload}'.");
            }
            finally
            {
                _sendGate.Release();
            }
        }

        private async Task RunKeepAliveAsync(ClientWebSocket socket, CancellationToken token)
        {
            if (_options.KeepAliveInterval <= TimeSpan.Zero)
            {
                return;
            }

            try
            {
                using var timer = new PeriodicTimer(_options.KeepAliveInterval);
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    if (socket.State != WebSocketState.Open)
                    {
                        return;
                    }

                    try
                    {
                        await _sendGate.WaitAsync(token).ConfigureAwait(false);
                        var buffer = Encoding.UTF8.GetBytes("ping");
                        await socket.SendAsync(new ArraySegment<Byte>(buffer), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsTransient(ex))
                    {
                        PluginLog.Warning(ex, "Websocket keep-alive ping failed.");
                        await ResetConnectionAsync().ConfigureAwait(false);
                        return;
                    }
                    finally
                    {
                        if (_sendGate.CurrentCount == 0)
                        {
                            _sendGate.Release();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposal cancels the token.
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Unexpected failure inside websocket keep-alive loop.");
                await ResetConnectionAsync().ConfigureAwait(false);
            }
        }

        private ClientWebSocket CreateClient()
        {
            var client = new ClientWebSocket();
            client.Options.KeepAliveInterval = _options.KeepAliveInterval;
            return client;
        }

        private async Task ResetConnectionAsync()
        {
            await _connectionGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await ResetConnectionCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        private async Task ResetConnectionCoreAsync()
        {
            var keepAliveCts = Interlocked.Exchange(ref _keepAliveCts, null);
            if (keepAliveCts is not null)
            {
                try
                {
                    keepAliveCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    keepAliveCts.Dispose();
                }
            }

            var keepAliveTask = Interlocked.Exchange(ref _keepAliveTask, null);
            if (keepAliveTask is not null)
            {
                try
                {
                    await keepAliveTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    PluginLog.Verbose(ex, "Keep-alive loop completed with an exception during reset.");
                }
            }

            var client = Interlocked.Exchange(ref _client, null);
            if (client is not null)
            {
                try
                {
                    if (client.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Verbose(ex, "Error while closing websocket connection.");
                }
                finally
                {
                    client.Dispose();
                }
            }
        }

        private static Boolean IsTransient(Exception exception)
            => exception is WebSocketException or IOException or TimeoutException;

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnectionManager));
            }
        }
    }
}
