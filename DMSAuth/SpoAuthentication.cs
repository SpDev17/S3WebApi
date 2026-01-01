using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SharePoint.Client;
using PnP.Core.Auth;
using PnP.Framework;
using S3WebApi.Helpers;

namespace S3WebApi.DMSAuth
{
    public class SpoAuthentication : ISpoAuthentication
    {
        private static string _sUserAgent;

        private readonly IConfiguration _configuration;
        private readonly ILogger<SpoAuthentication> _logger;
        private readonly AuthSecret _authSecret;
        private readonly HttpClient _httpClient;

        private readonly ConcurrentDictionary<string, ClientContext> _contexts;

        public SpoAuthentication(
            IConfiguration configuration,
            ILogger<SpoAuthentication> logger,
            AuthSecret authSecret,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _authSecret = authSecret;
            _httpClient = httpClient;

            _contexts = new ConcurrentDictionary<string, ClientContext>();
        }

        public ClientContext GetUserToken(string url, bool useCache = true)
        {
            var tenantIdKey = _configuration["AppPrincipal:ChinaTenantId"];
            var appIdKey = _configuration["AppPrincipal:ChinaAppId"];
            var pkKey = _configuration["AppPrincipal:ChinaCertificatePrivateKey"];
            var pfxKey = _configuration["AppPrincipal:ChinaPfx"];

            var cacheKey = url.ToLower().TrimEnd('/');

            if (useCache && _contexts.TryGetValue(cacheKey, out var context))
            {
               Console.WriteLine("Returning instance of China client context");

                return context;
            }

            _authSecret.Certificates.TryGetValue(tenantIdKey!, out var tenantId);
            _authSecret.Certificates.TryGetValue(appIdKey!, out var appId);
            _authSecret.Certificates.TryGetValue(pkKey!, out var privateKey);
            _authSecret.Certificates.TryGetValue(pfxKey!, out var pfxString);

            var rawData =
                Convert.FromBase64String(
                    pfxString!
                        .Replace("CERTIFICATE", "")
                        .Replace("BEGIN", "")
                        .Replace("END", "")
                        .Replace("-", "")
                        .Replace("\n", "")
                        .Replace(" ", ""));

            var cert = new X509Certificate2(rawData, privateKey);
            var authenticationManager = new AuthenticationManager(appId, cert, tenantId, null, AzureEnvironment.China);
            var clientContext = authenticationManager.GetContext(url);

            clientContext.ExecutingWebRequest += delegate (object _, WebRequestEventArgs e)
            {
                e.WebRequestExecutor.WebRequest.UserAgent = GetUserAgentDecoration();
            };

            clientContext.WebRequestExecutorFactory = new MShareHttpClientWebRequestExecutorFactory(_httpClient);

            if (useCache)
            {
                _contexts.TryAdd(cacheKey, clientContext);
            }

            return clientContext;
        }

        public ClientContext GetTransientUserToken(string url)
        {
            return GetUserToken(url, useCache: false);
        }

        public ClientContext GetUserTokenForEventRegistration(string url)
        {
            return GetUserToken(url, useCache: false);
        }

        public X509CertificateAuthenticationProvider GetCertificateAuthenticationProvider()
        {
            var tenantIdKey = _configuration["AppPrincipal:ChinaTenantId"];
            var appIdKey = _configuration["AppPrincipal:ChinaAppId"];
            var pkKey = _configuration["AppPrincipal:ChinaCertificatePrivateKey"];
            var pfxKey = _configuration["AppPrincipal:ChinaPfx"];

            _authSecret.Certificates.TryGetValue(tenantIdKey!, out var tenantId);
            _authSecret.Certificates.TryGetValue(appIdKey!, out var appId);
            _authSecret.Certificates.TryGetValue(pkKey!, out var privateKey);
            _authSecret.Certificates.TryGetValue(pfxKey!, out var pfxString);

            var rawData =
                Convert.FromBase64String(
                    pfxString!
                        .Replace("CERTIFICATE", "")
                        .Replace("BEGIN", "")
                        .Replace("END", "")
                        .Replace("-", "")
                        .Replace("\n", "")
                        .Replace(" ", ""));

            var cert = new X509Certificate2(rawData, privateKey);

            return new X509CertificateAuthenticationProvider(appId, tenantId, cert);
        }

        public ClientCertificateCredential GetClientCertificateCredential()
        {
            var tenantIdKey = _configuration["AppPrincipal:ChinaTenantId"];
            var appIdKey = _configuration["AppPrincipal:ChinaAppId"];
            var pkKey = _configuration["AppPrincipal:ChinaCertificatePrivateKey"];
            var pfxKey = _configuration["AppPrincipal:ChinaPfx"];

            _authSecret.Certificates.TryGetValue(tenantIdKey!, out var tenantId);
            _authSecret.Certificates.TryGetValue(appIdKey!, out var appId);
            _authSecret.Certificates.TryGetValue(pkKey!, out var privateKey);
            _authSecret.Certificates.TryGetValue(pfxKey!, out var pfxString);

            var rawData =
                Convert.FromBase64String(
                    pfxString!
                        .Replace("CERTIFICATE", "")
                        .Replace("BEGIN", "")
                        .Replace("END", "")
                        .Replace("-", "")
                        .Replace("\n", "")
                        .Replace(" ", ""));

            var cert = new X509Certificate2(rawData, privateKey);

            var options = new ClientCertificateCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzureChina
            };

            return new ClientCertificateCredential(tenantId, appId, cert, options);
        }

        private string GetUserAgentDecoration()
        {
            if (!string.IsNullOrEmpty(_sUserAgent))
            {
                return _sUserAgent;
            }

            _sUserAgent = new UserAgentDecorationHelper(_configuration).GetDecoration();

            Console.WriteLine("User agent decoration: {userAgent}", _sUserAgent);

            return _sUserAgent;
        }
    }
}
