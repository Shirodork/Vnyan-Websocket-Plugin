namespace Logitech.LogiActions.WebsocketPlugin.Helpers
{
    using System;

    internal sealed class WebSocketConnectionOptions
    {
        public WebSocketConnectionOptions(
            Uri endpoint,
            TimeSpan connectTimeout,
            TimeSpan sendTimeout,
            TimeSpan keepAliveInterval,
            Int32 maxRetryCount,
            TimeSpan retryBaseDelay,
            TimeSpan retryMaxDelay)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            ConnectTimeout = connectTimeout > TimeSpan.Zero ? connectTimeout : throw new ArgumentOutOfRangeException(nameof(connectTimeout));
            SendTimeout = sendTimeout > TimeSpan.Zero ? sendTimeout : throw new ArgumentOutOfRangeException(nameof(sendTimeout));
            KeepAliveInterval = keepAliveInterval < TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(keepAliveInterval)) : keepAliveInterval;
            MaxRetryCount = maxRetryCount > 0 ? maxRetryCount : throw new ArgumentOutOfRangeException(nameof(maxRetryCount));
            RetryBaseDelay = retryBaseDelay > TimeSpan.Zero ? retryBaseDelay : throw new ArgumentOutOfRangeException(nameof(retryBaseDelay));
            RetryMaxDelay = retryMaxDelay >= retryBaseDelay ? retryMaxDelay : throw new ArgumentOutOfRangeException(nameof(retryMaxDelay));
        }

        public Uri Endpoint { get; }

        public TimeSpan ConnectTimeout { get; }

        public TimeSpan SendTimeout { get; }

        public TimeSpan KeepAliveInterval { get; }

        public Int32 MaxRetryCount { get; }

        public TimeSpan RetryBaseDelay { get; }

        public TimeSpan RetryMaxDelay { get; }

        public TimeSpan GetRetryDelay(Int32 attempt)
        {
            if (attempt <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attempt));
            }

            var multiplier = Math.Pow(2, Math.Max(0, attempt - 1));
            var milliseconds = RetryBaseDelay.TotalMilliseconds * multiplier;
            var capped = Math.Min(milliseconds, RetryMaxDelay.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(capped);
        }

        public WebSocketConnectionOptions WithEndpoint(Uri endpoint)
            => new(endpoint, ConnectTimeout, SendTimeout, KeepAliveInterval, MaxRetryCount, RetryBaseDelay, RetryMaxDelay);

        public WebSocketConnectionOptions WithRetryPolicy(Int32 maxRetryCount, TimeSpan retryBaseDelay, TimeSpan retryMaxDelay)
            => new(Endpoint, ConnectTimeout, SendTimeout, KeepAliveInterval, maxRetryCount, retryBaseDelay, retryMaxDelay);

        public WebSocketConnectionOptions WithTimeouts(TimeSpan connectTimeout, TimeSpan sendTimeout)
            => new(Endpoint, connectTimeout, sendTimeout, KeepAliveInterval, MaxRetryCount, RetryBaseDelay, RetryMaxDelay);

        public WebSocketConnectionOptions WithKeepAlive(TimeSpan keepAlive)
            => new(Endpoint, ConnectTimeout, SendTimeout, keepAlive, MaxRetryCount, RetryBaseDelay, RetryMaxDelay);
    }
}
