using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.SharePoint.Client;
using PnP.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using PnP.Core.Auth;
using S3WebApi.Helpers;
using S3WebApi.Interfaces;
using Microsoft.Identity.Client;

namespace S3WebApi.DMSAuth;

public class DMSAuthOperation : IDMSAuthOperation
{
    private static bool s_hasLoggedCert;
    private static string s_userAgent;
    private const string THROTTLING_MANAGEMENT_FEATURE_CODE = "throttle-management";

    protected readonly IConfiguration Configuration;
    private Serilog.ILogger _logger => Serilog.Log.ForContext<DMSAuthOperation>();
    private readonly IMemoryCache _cache;
    private readonly AuthSecret _authSecret;
    private readonly ConcurrentDictionary<string, ClientContext> contexts;
    private readonly HttpClient _httpClient;
    private readonly IFeatureFlagDataPort _featureFlag;
    private readonly ISpoAuthentication _spoAuthentication;
    private readonly IChinaTermMappingSwitcher _chinaTermMappingSwitcher;

    public DMSAuthOperation(
        IConfiguration configuration,
        IMemoryCache memoryCache,
        AuthSecret authSecret,
        HttpClient httpClient,
        IFeatureFlagDataPort featureFlag,
        ISpoAuthentication spoAuthentication,
        IChinaTermMappingSwitcher chinaTermMappingSwitcher)
    {
        Configuration = configuration;
        _cache = memoryCache;
        _authSecret = authSecret;
        contexts = new ConcurrentDictionary<string, ClientContext>();
        _httpClient = httpClient;
        _featureFlag = featureFlag;
        _spoAuthentication = spoAuthentication;
        _chinaTermMappingSwitcher = chinaTermMappingSwitcher;
    }

    public ClientContext getUserToken(string url, string userId, bool useCache = true, string spnName = null)
    {
        _logger.AddMethodName().Information("Getting ClientContext for: {url}", url);

        if (IsChinaSpoHost(url))
        {
            _chinaTermMappingSwitcher.Switch();

            return _spoAuthentication.GetUserToken(url, useCache);
        }

        string tenantId = Configuration["AppPrincipal:TenantID"];
        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdString);
        string privateKey = Configuration["AppPrincipal:PrivateKey"];
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyString);

        var cacheKey = url.ToLower().TrimEnd('/');
        if (useCache && contexts.ContainsKey(cacheKey))
        {
            Console.WriteLine("Returning instance of ClientContext");

            return contexts[cacheKey];
        }

        Console.WriteLine("Creating new instance of ClientContext");

        bool isThrottlingManagementEnabled = _featureFlag.IsFeatureEnabled(THROTTLING_MANAGEMENT_FEATURE_CODE);

        KeyValuePair<string, string> certKeyValue;
        if (string.IsNullOrEmpty(spnName) && isThrottlingManagementEnabled)
        {
            certKeyValue = _authSecret.GetDesignatedCert(Configuration);
            LogSpnOnce(certKeyValue);
        }
        else
        {
            do
            {
                certKeyValue = _authSecret.NextMap();
            } while (!string.IsNullOrEmpty(spnName) && certKeyValue.Value != spnName);
        }

        _authSecret.Certificates.TryGetValue(certKeyValue.Value + ".pfx", out var pfxString);
        byte[] rawData = Convert.FromBase64String(pfxString.Replace("CERTIFICATE", "").Replace("BEGIN", "").Replace("END", "").Replace("-", "").Replace("\n", "").Replace(" ", ""));
        var cert = new X509Certificate2(rawData, privateKeyString);
        Console.WriteLine("Using Cert key: {0} value: {1}", certKeyValue.Key, certKeyValue.Value);

        var appId = certKeyValue.Key;
        var authenticationManager = new AuthenticationManager(appId, cert, tenantIdString);

        var clientContext = authenticationManager.GetContext(url);
        clientContext.RequestTimeout = int.Parse(Configuration["SpoRequestTimeoutInMilliseconds"]!);
        clientContext.ExecutingWebRequest += delegate (object _, WebRequestEventArgs e)
        {
            e.WebRequestExecutor.WebRequest.UserAgent = isThrottlingManagementEnabled
                ? GetUserAgentDecoration()
                : Configuration["DynamicConfigurations:DefaultUserAgent"];
        };

        if (isThrottlingManagementEnabled)
        {
            // Hookup custom WebRequestExecutorFactory for throttling management
            clientContext.WebRequestExecutorFactory = new MShareHttpClientWebRequestExecutorFactory(_httpClient);
        }

        Console.WriteLine("Created New Context");

        if (useCache)
        {
            contexts.TryAdd(cacheKey, clientContext);
        }

        return clientContext;
    }

    public ClientContext getTransientUserToken(string url, string userId, string spnName = null)
    {
        _logger.AddMethodName().Information("Getting ClientContext for: {url}", url);

        if (IsChinaSpoHost(url))
        {
            _chinaTermMappingSwitcher.Switch();

            return _spoAuthentication.GetTransientUserToken(url);
        }

        string tenantId = Configuration["AppPrincipal:TenantID"];
        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdString);
        string privateKey = Configuration["AppPrincipal:PrivateKey"];
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyString);

        Console.WriteLine("Creating new instance of ClientContext");

        bool isThrottlingManagementEnabled = _featureFlag.IsFeatureEnabled(THROTTLING_MANAGEMENT_FEATURE_CODE);

        KeyValuePair<string, string> certKeyValue;
        if (string.IsNullOrEmpty(spnName) && isThrottlingManagementEnabled)
        {
            certKeyValue = _authSecret.GetDesignatedCert(Configuration);
            LogSpnOnce(certKeyValue);
        }
        else
        {
            do
            {
                certKeyValue = _authSecret.NextMap();
            } while (!string.IsNullOrEmpty(spnName) && certKeyValue.Value != spnName);
        }

        _authSecret.Certificates.TryGetValue(certKeyValue.Value + ".pfx", out var pfxString);
        byte[] rawData = Convert.FromBase64String(pfxString.Replace("CERTIFICATE", "").Replace("BEGIN", "").Replace("END", "").Replace("-", "").Replace("\n", "").Replace(" ", ""));
        var cert = new X509Certificate2(rawData, privateKeyString);
        Console.WriteLine("Using Cert key: {0} value: {1}", certKeyValue.Key, certKeyValue.Value);

        var appId = certKeyValue.Key;
        var authenticationManager = new AuthenticationManager(appId, cert, tenantIdString);

        var clientContext = authenticationManager.GetContext(url);
        clientContext.RequestTimeout = int.Parse(Configuration["SpoRequestTimeoutInMilliseconds"]!);
        clientContext.ExecutingWebRequest += delegate (object _, WebRequestEventArgs e)
        {
            e.WebRequestExecutor.WebRequest.UserAgent = isThrottlingManagementEnabled
                ? GetUserAgentDecoration()
                : Configuration["DynamicConfigurations:DefaultUserAgent"];
        };

        if (isThrottlingManagementEnabled)
        {
            // Hookup custom WebRequestExecutorFactory for throttling management
            clientContext.WebRequestExecutorFactory = new MShareHttpClientWebRequestExecutorFactory(_httpClient);
        }

        Console.WriteLine("Created New Context");

        return clientContext;
    }

    public ClientContext getUserTokenForEventRegistration(string url)
    {
        if (IsChinaSpoHost(url))
        {
            _chinaTermMappingSwitcher.Switch();

            return _spoAuthentication.GetUserTokenForEventRegistration(url);
        }

        string tenantId = Configuration["AppPrincipal:TenantID"];
        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdString);
        string appId = Configuration["LibEventRegistration:AppIDTobeUsed"];
        string certToUse = Configuration["LibEventRegistration:certTobeUsed"];
        string privateKey = Configuration["AppPrincipal:PrivateKey"];
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyString);
        string userTokenCtxCacheKey = "ContextCacheKeyrEventReg" + url;
        var useCert = new KeyValuePair<string, string>();
        if (_cache.Get(userTokenCtxCacheKey) == null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(120));

            foreach (var certMap in _authSecret.Mapping)
            {
                if (certMap.Value.ToLower().Equals(certToUse.ToLower()))
                {
                    useCert = certMap;
                    break;
                }
            }
            _authSecret.Certificates.TryGetValue(useCert.Value + ".pfx", out string pfxString);
            byte[] rawData = Convert.FromBase64String(pfxString.Replace("CERTIFICATE", "").Replace("BEGIN", "").Replace("END", "").Replace("-", "").Replace("\n", "").Replace(" ", ""));
            var cert = new X509Certificate2(rawData, privateKeyString);
            Console.WriteLine("Using Cert key: {0} value: {1}", useCert.Key, useCert.Value);
            var authenticationManager = new AuthenticationManager(appId, cert, tenantIdString);
            using (var cc = authenticationManager.GetContext(url))
            {
                cc.ExecutingWebRequest += delegate (object sender, WebRequestEventArgs e)
                {
                    e.WebRequestExecutor.WebRequest.UserAgent = Configuration["DynamicConfigurations:DefaultUserAgent"];
                };

                _cache.Set(userTokenCtxCacheKey, cc, cacheEntryOptions);
                Console.WriteLine("Created New Context ");
                return cc;
            }
        }
        else
        {
            var cc = (ClientContext)_cache.Get(userTokenCtxCacheKey);
            return cc;
        }
    }

    public X509CertificateAuthenticationProvider GetCertificateAuthenticationProvider(string url, string spnName = null)
    {
        Console.WriteLine("Getting X509Certificate2: {0}", spnName);

        if (IsChinaSpoHost(url))
        {
            _chinaTermMappingSwitcher.Switch();

            return _spoAuthentication.GetCertificateAuthenticationProvider();
        }

        var tenantId = Configuration["AppPrincipal:TenantID"];
        var privateKey = Configuration["AppPrincipal:PrivateKey"];

        var isThrottlingManagementEnabled = _featureFlag.IsFeatureEnabled(THROTTLING_MANAGEMENT_FEATURE_CODE);

        KeyValuePair<string, string> certKeyValue;

        if (string.IsNullOrEmpty(spnName) && isThrottlingManagementEnabled)
        {
            certKeyValue = _authSecret.GetDesignatedCert(Configuration);
            LogSpnOnce(certKeyValue);
        }
        else
        {
            do
            {
                certKeyValue = _authSecret.NextMap();
            } while (!string.IsNullOrEmpty(spnName) && certKeyValue.Value != spnName);
        }

        var (appId, value) = certKeyValue;

        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdValue);
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyValue);
        _authSecret.Certificates.TryGetValue($"{value}.pfx", out var pfxValue);

        if (pfxValue == null)
            throw new InvalidOperationException();

        var data = Convert.FromBase64String(
            pfxValue
                .Replace("CERTIFICATE", string.Empty)
                .Replace("BEGIN", string.Empty)
                .Replace("END", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty));

        var cert = new X509Certificate2(data, privateKeyValue);

        return new X509CertificateAuthenticationProvider(appId, tenantIdValue, cert);
    }

    public ClientCertificateCredential GetClientCertificateCredential(string spnName = null)
    {
        Console.WriteLine("Getting X509Certificate2: {0}", spnName);

        var tenantId = Configuration["AppPrincipal:TenantID"];
        var privateKey = Configuration["AppPrincipal:PrivateKey"];

        var isThrottlingManagementEnabled = _featureFlag.IsFeatureEnabled(THROTTLING_MANAGEMENT_FEATURE_CODE);

        KeyValuePair<string, string> certKeyValue;

        if (string.IsNullOrEmpty(spnName) && isThrottlingManagementEnabled)
        {
            certKeyValue = _authSecret.GetDesignatedCert(Configuration);
            LogSpnOnce(certKeyValue);
        }
        else
        {
            do
            {
                certKeyValue = _authSecret.NextMap();
            } while (!string.IsNullOrEmpty(spnName) && certKeyValue.Value != spnName);
        }

        var (appId, value) = certKeyValue;

        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdValue);
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyValue);
        _authSecret.Certificates.TryGetValue($"{value}.pfx", out var pfxValue);

        if (pfxValue == null)
            throw new InvalidOperationException();

        var data = Convert.FromBase64String(
            pfxValue
                .Replace("CERTIFICATE", string.Empty)
                .Replace("BEGIN", string.Empty)
                .Replace("END", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty));

        var cert = new X509Certificate2(data, privateKeyValue);

        var options = new ClientCertificateCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        return new ClientCertificateCredential(tenantIdValue, appId, cert, options);
    }

    private void LogSpnOnce(KeyValuePair<string, string> cert)
    {
        if (s_hasLoggedCert)
        {
            return;
        }
        s_hasLoggedCert = true;

        var podName = Environment.GetEnvironmentVariable("POD_NAME");
        if (string.IsNullOrEmpty(podName))
        {
            //Console.WriteLine("ClientContext acquired using SPN {spnName}, {appId}",
            //    cert.Value, cert.Key);
        }
        else
        {
            //Console.WriteLine("ClientContext acquired for pod {podName} using SPN {spnName}, {appId}",
            //    podName, cert.Value, cert.Key);
        }
    }

    private string GetUserAgentDecoration()
    {
        if (!string.IsNullOrEmpty(s_userAgent))
        {
            return s_userAgent;
        }

        s_userAgent = new UserAgentDecorationHelper(Configuration).GetDecoration();
        Console.WriteLine("User agent decoration: {userAgent}", s_userAgent);

        return s_userAgent;
    }

    private bool IsChinaSpoHost(string url)
    {
        var spHost = Configuration["SharePointChinaHost"];
        var uri = new Uri(url);

        return uri.Host.EndsWith(spHost!);
    }

    public async Task<string> AcquireTokenWithCertificateAsync(string spnName = null, string scope = null)
    {
        var tenantId = Configuration["AppPrincipal:TenantID"];
        var privateKey = Configuration["AppPrincipal:PrivateKey"];
        var isThrottlingManagementEnabled = _featureFlag.IsFeatureEnabled(THROTTLING_MANAGEMENT_FEATURE_CODE);
        KeyValuePair<string, string> certKeyValue;
        if (string.IsNullOrEmpty(spnName) && isThrottlingManagementEnabled)
        {
            certKeyValue = _authSecret.GetDesignatedCert(Configuration);
            LogSpnOnce(certKeyValue);
        }
        else
        {
            do
            {
                certKeyValue = _authSecret.NextMap();
            } while (!string.IsNullOrEmpty(spnName) && certKeyValue.Value != spnName);
        }

        var (appId, value) = certKeyValue;
        _authSecret.Certificates.TryGetValue(tenantId, out var tenantIdValue);
        _authSecret.Certificates.TryGetValue(privateKey, out var privateKeyValue);
        _authSecret.Certificates.TryGetValue($"{value}.pfx", out var pfxValue);
        if (pfxValue == null)
            throw new InvalidOperationException();

        var data = Convert.FromBase64String(
            pfxValue
                .Replace("CERTIFICATE", string.Empty)
                .Replace("BEGIN", string.Empty)
                .Replace("END", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty));

        var cert = new X509Certificate2(data, privateKeyValue);
        //"https://yddr5.sharepoint.com/.default"
        //"https://graph.microsoft.com/.default"
        if (string.IsNullOrEmpty(scope))
        {
            scope = "https://graph.microsoft.com";
        }
        string[] scopes = new[] { scope + "/.default" };
        var app = ConfidentialClientApplicationBuilder
                    .Create(appId)
                    .WithCertificate(cert)
                    .WithTenantId(tenantIdValue).WithAuthority(AzureCloudInstance.None, tenantIdValue)
                    .Build();

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
}
