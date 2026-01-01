using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using S3WebApi.DMSAuth;
using S3WebApi.Helpers;
using S3WebApi.Interfaces;

namespace S3WebApi.Repository
{
    public class GraphClient
    {
        private readonly IConfiguration _configuration;
        private readonly ISpoAuthentication _spoAuthentication;
        private readonly IFeatureFlagDataPort _featureFlag;
        private readonly HttpClient _httpClient;

        private readonly object _graphLock = new();
        private GraphServiceClient? _chinaInstance;

        public GraphClient(
            IConfiguration configuration,
            ISpoAuthentication spoAuthentication,
            IDMSAuthOperation authenticationProvider,
            HttpClient httpClient,
            IFeatureFlagDataPort featureFlag)
        {
            _configuration = configuration;
            _spoAuthentication = spoAuthentication;
            _httpClient = httpClient;
            _featureFlag = featureFlag;

            var handlers = GraphClientFactory.CreateDefaultHandlers();

            handlers.Add(new ThrottlingHandler(configuration, featureFlag) { InnerHandler = new HttpClientHandler() });

            var client = GraphClientFactory.Create(handlers);

            Instance = new GraphServiceClient(client, authenticationProvider.GetClientCertificateCredential());
        }

        private GraphServiceClient Instance { get; }

        private GraphServiceClient ChinaInstance
        {
            get
            {
                if (_chinaInstance != null)
                {
                    return _chinaInstance;
                }

                var chinaHandlers = GraphClientFactory.CreateDefaultHandlers();

                chinaHandlers.Add(
                    new ThrottlingHandler(_configuration, _featureFlag)
                    {
                        InnerHandler = new HttpClientHandler()
                    });

                var chinaClient = GraphClientFactory.Create(chinaHandlers);

                lock (_graphLock)
                {
                    _chinaInstance ??= new GraphServiceClient(
                        chinaClient,
                        _spoAuthentication.GetClientCertificateCredential(),
                        null,
                        _configuration["SharePointChinaGraphEndpoint"]);
                }

                return _chinaInstance;
            }
        }

        public GraphServiceClient SpoInstance(string url)
        {
            return IsChinaSpoHost(url) ? ChinaInstance : Instance;
        }

        private bool IsChinaSpoHost(string url)
        {
            var spHost = _configuration["SharePointChinaHost"];
            var uri = new Uri(url);

            return uri.Host.EndsWith(spHost!);
        }
    }
}
