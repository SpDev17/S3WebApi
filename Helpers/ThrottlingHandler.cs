using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using PnP.Framework.Diagnostics;
using S3WebApi.DMSAuth;
using S3WebApi.Exceptions;

namespace S3WebApi.Helpers
{
    public sealed class ThrottlingHandler : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IFeatureFlagDataPort _featureFlag;
        private const string RETRY_AFTER = "Retry-After";
        private const int DEFAULT_THROTTLE_WAIT_TIME_IN_SEC = 15;
        private const int ADDITIONAL_THROTTLE_WAIT_TIME_IN_SEC = 5;

        private const string CsomSearchMarker = "Search/_vti_bin/client.svc/ProcessQuery";

        // Create a rate limiter inside the handler as we only instantiate this handler once
        private readonly RateLimiter? _rateLimiter;

        //private readonly ILogger _logger = Log.ForContext<ThrottlingHandler>();

        public int MaxRetries { get; set; } = 2;

        public ThrottlingHandler(IConfiguration configuration, IFeatureFlagDataPort featureFlag, bool useRateLimiter = true)
        {
            _configuration = configuration;
            _featureFlag = featureFlag;

            if (useRateLimiter)
            {
                _rateLimiter = new();
            }
        }

        public ThrottlingHandler(bool useRateLimiter = true)
        {
            if (useRateLimiter)
            {
                _rateLimiter = new();
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (true)
            {
                // Throw an exception if we've requested to cancel the operation
                cancellationToken.ThrowIfCancellationRequested();

                // Understand which API is being called
                string apiType = GetApiType(request.RequestUri.ToString());

                // Check if we need to delay this request due to rate limiting
                if (_rateLimiter != null)
                {
                    await _rateLimiter.WaitAsync(apiType, cancellationToken);
                }

                Stopwatch? stopwatch = null;

                if (_featureFlag != null && _featureFlag.IsFeatureEnabled("capture-spo-request-uri"))
                {
                    Console.WriteLine("SPO request detail -> Request: {request}", request.RequestUri.Host);
                }

                if (_featureFlag != null && _featureFlag.IsFeatureEnabled("log-spo-execution-time"))
                {
                    stopwatch = new Stopwatch();

                    stopwatch.Start();
                }

                // Issue the request
                var response = await base.SendAsync(request, cancellationToken);

                if (stopwatch != null)
                {
                    stopwatch.Stop();

                    Console.WriteLine("SPO execution took -> MS: {ms}, Type: {type}", stopwatch.ElapsedMilliseconds, apiType);
                }

                // Log Graph request
                if (_configuration != null)
                {
                    var ignoreStatusCodes = _configuration.GetSection("SpoIgnoreHttpStatusCodesInLogging").Get<int[]>();

                    if (!ignoreStatusCodes.Contains((int)response.StatusCode))
                    {
                        Console.WriteLine("SPO HTTP response -> Type: {type}, Status: {status}, Headers: {@headers}",
                            apiType, response.StatusCode, response.Headers);
                    }
                }

                // Log SPO CSOM search calls!
                if (_featureFlag != null && _featureFlag.IsFeatureEnabled("log-spo-csom-search-call") &&
                    request.Method == HttpMethod.Post &&
                    request.RequestUri.AbsolutePath.EndsWith(CsomSearchMarker, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("SPO CSOM response -> Status: {status}, Headers: {@headers}",
                        response.StatusCode, response.Headers);
                }

                // Inspect the response headers for RateLimit headers
                _rateLimiter?.UpdateWindow(response, apiType);

                // If the request does not require a retry then we're done
                if (!ShouldRetry(response.StatusCode))
                {
                    return response;
                }

                // Drain response content to free connections. Need to perform this
                // before retry attempt and before the TooManyRetries ServiceException.
                if (response?.Content != null)
                {
                    await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }

                // Safety measure to not keep retrying forever
                if (retryCount >= MaxRetries)
                {
                    throw new DmsThrottleException($"{apiType} request reached it's max retry count of {retryCount}");
                }

                // Prepare Delay task configured with the delay time from response's Retry-After header
                var waitTime = CalculateWaitTime(response);
                Task delay = Task.Delay(waitTime, cancellationToken);

                // Clone request with CloneAsync before retrying
                // Do not dispose this request as that breaks the request cloning
                request = await CloneAsync(request);

                // Increase retryCount
                retryCount++;

                Console.WriteLine("WARNING: Throttling {apiType} request, waiting for {waitTimeInSeconds} seconds (includes buffer of {bufferInSeconds} seconds)",
                    apiType,
                    waitTime.Seconds,
                    ADDITIONAL_THROTTLE_WAIT_TIME_IN_SEC);

                // Delay time based upon Retry-After header
                await delay;
            }
        }

        private static string GetApiType(string requestUri)
        {
            if (!string.IsNullOrEmpty(requestUri))
            {
                if (requestUri.Contains("_api"))
                {
                    return "SharePoint REST";
                }
                else if (requestUri.Contains("_vti_bin"))
                {
                    return "SharePoint CSOM";
                }
                else if (requestUri.Contains("graph.microsoft.com"))
                {
                    return "Microsoft Graph";
                }
            }

            return "?";
        }

        private static TimeSpan CalculateWaitTime(HttpResponseMessage? response)
        {
            int delayInSeconds = DEFAULT_THROTTLE_WAIT_TIME_IN_SEC;

            if (response != null && response.Headers.TryGetValues(RETRY_AFTER, out var values))
            {
                // Can we use the provided retry-after header?
                string retryAfter = values.First();
                if (int.TryParse(retryAfter, out var delaySeconds))
                {
                    delayInSeconds = delaySeconds + ADDITIONAL_THROTTLE_WAIT_TIME_IN_SEC;
                }
            }

            return TimeSpan.FromSeconds(delayInSeconds);
        }

        internal static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return (statusCode == HttpStatusCode.ServiceUnavailable ||
                    statusCode == HttpStatusCode.GatewayTimeout ||
                    statusCode == (HttpStatusCode)429);
        }

        /// <summary>
        /// Create a new HTTP request by copying previous HTTP request's headers and properties from response's request message.
        /// Copied from: https://github.com/microsoftgraph/msgraph-sdk-dotnet-core/blob/dev/src/Microsoft.Graph.Core/Extensions/HttpRequestMessageExtensions.cs
        /// </summary>
        /// <param name="originalRequest">The previous <see cref="HttpRequestMessage"/> needs to be copy.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        /// <remarks>
        /// Re-issue a new HTTP request with the previous request's headers and properities
        /// </remarks>
        internal static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage originalRequest)
        {
            var newRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);

            // Copy request headers.
            foreach (var header in originalRequest.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy request properties.
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var property in originalRequest.Properties)
            {
                newRequest.Properties.Add(property);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            // Set Content if previous request had one.
            if (originalRequest.Content != null)
            {
                // HttpClient doesn't rewind streams and we have to explicitly do so.
                await originalRequest.Content.ReadAsStreamAsync().ContinueWith(t =>
                {
                    if (t.Result.CanSeek)
                    {
                        t.Result.Seek(0, SeekOrigin.Begin);
                    }

                    newRequest.Content = new StreamContent(t.Result);
                }).ConfigureAwait(false);

                // Copy content headers.
                if (originalRequest.Content.Headers != null)
                {
                    foreach (var contentHeader in originalRequest.Content.Headers)
                    {
                        newRequest.Content.Headers.TryAddWithoutValidation(contentHeader.Key, contentHeader.Value);
                    }
                }
            }

            return newRequest;
        }
    }
}
