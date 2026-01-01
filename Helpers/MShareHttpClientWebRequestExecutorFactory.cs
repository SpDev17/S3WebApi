using Microsoft.SharePoint.Client;

namespace S3WebApi.Helpers
{
    public sealed class MShareHttpClientWebRequestExecutorFactory : WebRequestExecutorFactory
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Creates a WebRequestExecutorFactory that utilizes the specified HttpClient
        /// </summary>
        /// <param name="httpClientInstance">HttpClient to use when creating new web requests</param>
        public MShareHttpClientWebRequestExecutorFactory(HttpClient httpClientInstance)
        {
            httpClient = httpClientInstance;
        }

        /// <summary>
        /// Creates a WebRequestExecutor that utilizes HttpClient
        /// </summary>
        /// <param name="context">A SharePoint ClientContext</param>
        /// <param name="requestUrl">The url to create the request for</param>
        /// <returns>A WebRequestExecutor object created for the passed site URL</returns>
        public override WebRequestExecutor CreateWebRequestExecutor(ClientRuntimeContext context, string requestUrl)
        {
            return new HttpClientSPWebRequestExecutor(httpClient, context, requestUrl);
        }
    }
}
