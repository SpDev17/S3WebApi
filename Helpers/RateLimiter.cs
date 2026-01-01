using Microsoft.Extensions.Logging;
using PnP.Framework.Diagnostics;

namespace S3WebApi.Helpers
{
    internal sealed class RateLimiter
    {
        private const string RATELIMIT_LIMIT = "RateLimit-Limit";
        private const string RATELIMIT_REMAINING = "RateLimit-Remaining";
        private const string RATELIMIT_RESET = "RateLimit-Reset";
        private const int ADDITIONAL_WAIT_TIME_IN_SEC = 5;

        /// <summary>
        /// Lock for controlling Read/Write access to the variables.
        /// </summary>
        private readonly ReaderWriterLockSlim _readerWriterLock = new();

        /// <summary>
        /// Maximum number of requests per window
        /// </summary>
        private int _limit;

        /// <summary>
        /// The time, in <see cref="TimeSpan.Seconds"/>, when the current window gets reset
        /// </summary>
        private int _reset;

        /// <summary>
        /// The timestamp when current window will be reset, in <see cref="TimeSpan.Ticks"/>.
        /// </summary>
        private long _nextReset;

        /// <summary>
        /// The remaining requests in the current window.
        /// </summary>
        private int _remaining;

        /// <summary>
        /// Minimum % of requests left before the next request will get delayed until the current window is reset
        /// Feel free to experiment with this number to find the optimal value for your scenario
        /// </summary>
        internal int MinimumCapacityLeft { get; set; } = 10;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RateLimiter()
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                _ = Interlocked.Exchange(ref _limit, -1);
                _ = Interlocked.Exchange(ref _remaining, -1);
                _ = Interlocked.Exchange(ref _reset, -1);
                _ = Interlocked.Exchange(ref _nextReset, DateTime.UtcNow.Ticks);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// If needed delay the execution of an HTTP request
        /// </summary>
        /// <param name="apiType">Type of API that we're possibly delaying (for logging purposes only)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        internal async Task WaitAsync(string apiType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We're not using the rate limiter
            if (MinimumCapacityLeft == 0)
            {
                return;
            }

            long delayInTicks = 0;
            float capacityLeft = 0;
            _readerWriterLock.EnterReadLock();
            try
            {
                // Remaining = 0 means the request is throttled and there's a retry-after header that will be used
                if (_limit > 0 && _remaining > 0)
                {
                    // Calculate percentage requests left in the current window
                    capacityLeft = (float)Math.Round((float)_remaining / _limit * 100, 2);

                    // If getting below the minimum required capacity then lets wait until the current window is reset
                    if (capacityLeft <= MinimumCapacityLeft)
                    {
                        delayInTicks = _nextReset - DateTime.UtcNow.Ticks;
                    }
                    else
                    {
                        Console.WriteLine("Rate Limit capacity left: {capacityLeft}% is still above the threshold {minCapacity}%. We are not delaying the request just yet.",
                            capacityLeft, MinimumCapacityLeft);
                    }
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }

            if (delayInTicks > 0)
            {
                var delayInTicksWithBuffer = delayInTicks + (TimeSpan.TicksPerSecond * ADDITIONAL_WAIT_TIME_IN_SEC);

                Console.WriteLine("Delaying {apiType} request for {delayInSeconds} seconds, capacity left: {capacityLeft}%",
                    apiType, new TimeSpan(delayInTicksWithBuffer).Seconds, capacityLeft);

                await Task.Delay(new TimeSpan(delayInTicksWithBuffer), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks for RateLimit headers and if so processes them
        /// </summary>
        /// <param name="response">Respose from the HTTP request</param>
        /// <param name="apiType">Type of API that we're possibly delaying (for logging purposes only)</param>
        internal void UpdateWindow(HttpResponseMessage? response, string apiType)
        {
            int rateLimit = -1;
            int rateRemaining = -1;
            int rateReset = -1;

            // We're not using the rate limiter
            if (MinimumCapacityLeft == 0)
            {
                return;
            }

            if (response == null)
            {
                return;
            }

            if (response.Headers.TryGetValues(RATELIMIT_LIMIT, out var limitValues))
            {
                string rateString = limitValues.First();
                _ = int.TryParse(rateString, out rateLimit);
            }

            if (response.Headers.TryGetValues(RATELIMIT_REMAINING, out var remainingValues))
            {
                string rateString = remainingValues.First();
                _ = int.TryParse(rateString, out rateRemaining);
            }

            if (response.Headers.TryGetValues(RATELIMIT_RESET, out var resetValues))
            {
                string rateString = resetValues.First();
                _ = int.TryParse(rateString, out rateReset);
            }

            _readerWriterLock.EnterWriteLock();
            try
            {
                _ = Interlocked.Exchange(ref _limit, rateLimit);
                _ = Interlocked.Exchange(ref _remaining, rateRemaining);
                _ = Interlocked.Exchange(ref _reset, rateReset);

                if (rateReset > -1)
                {
                    // Track when the current window get's reset
                    _ = Interlocked.Exchange(ref _nextReset, DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(rateReset).Ticks);
                }
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }

            if (rateReset > -1)
            {
                Console.WriteLine("RateLimit received for {apiType} request. RateLimit-Limit: {rateLimit}, RateLimit-Remaining: {rateRemaining}, RateLimit-Reset: {rateReset}",
                    apiType, rateLimit, rateRemaining, rateReset);
            }
        }
    }
}
