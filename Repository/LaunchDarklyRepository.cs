using Microsoft.Extensions.Logging;
using S3WebApi.DMSAuth;
using S3WebApi.Helpers;
using System.Net.Sockets;
using S3WebApi.Interfaces;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace S3WebApi.Repository;

public class LaunchDarklyRepository : IFeatureFlagDataPort
{
    private const string ANY_SYSTEM_CODE = "any-system-code";
    private const string SEGMENT_ORIGINATING_SYSTEM_CODE = "originating-system-code";
    private const string SEGMENT_SITE_COLLECTION_URL = "site-collection-url";

    private readonly LdClient? _ldClient;

    public LaunchDarklyRepository(AuthSecret authSecret, IConfiguration configuration)
    {
        authSecret.RequireNotNull(nameof(authSecret));
        configuration.RequireNotNull(nameof(configuration));

        var vaultKey = configuration["LaunchDarkly:SdkVaultKey"];
        if (!authSecret.Certificates.TryGetValue(vaultKey, out var apiKey))
        {
            Console.WriteLine("LaunchDarkly key {vaultKey} not found", vaultKey);
            return;
        }

        try
        {
            var ldConfig = Configuration.Builder(apiKey)
                .Logging(Components.NoLogging)
                .Build();
            _ldClient = new LdClient(ldConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to initialize LaunchDarkly client: {error}", ex.Message);
        }
    }

    public bool IsFeatureEnabled(
        string featureName, bool defaultValue = false, string? originatingSystemCode = null, string? siteCollectionUrl = null)
    {
        featureName.RequireNotNullOrEmpty(nameof(featureName));

        if (!EnsureInitialised(featureName, defaultValue))
        {
            return defaultValue;
        }

        try
        {
            var targetSystem = !string.IsNullOrEmpty(originatingSystemCode)
                ? originatingSystemCode
                : ANY_SYSTEM_CODE;
            var contexts = new List<Context>
                {
                    Context
                        .Builder(targetSystem)
                        .Kind(SEGMENT_ORIGINATING_SYSTEM_CODE)
                        .Build()
                };

            if (!string.IsNullOrEmpty(siteCollectionUrl))
            {
                contexts.Add(Context
                    .Builder(siteCollectionUrl)
                    .Kind(SEGMENT_SITE_COLLECTION_URL)
                    .Build());
            }

            var context = Context.NewMulti(contexts.ToArray());

#pragma warning disable CS8602
            var flagValue = _ldClient.BoolVariation(featureName, context, defaultValue);
#pragma warning restore CS8602
            return flagValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while evaluating LaunchDarkly feature {featureName}. Return default value {default}: {error}",
                featureName, defaultValue, ex.Message);
            return defaultValue;
        }
    }

    public string? GetFeatureValue(string featureName, string? defaultValue = null)
    {
        featureName.RequireNotNullOrEmpty(nameof(featureName));

        if (!EnsureInitialised(featureName, defaultValue))
        {
            return defaultValue;
        }

        try
        {
            var context = Context
                .Builder("any-system-code")
                .Kind("originating-system-code")
                .Build();

#pragma warning disable CS8602
            var flagValue = _ldClient.StringVariation(featureName, context, defaultValue);
#pragma warning restore CS8602
            return flagValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while evaluating LaunchDarkly feature {featureName}. Return default value {default}: {error}",
                featureName, defaultValue, ex.Message);
            return defaultValue;
        }
    }

    private bool EnsureInitialised<T>(string featureName, T defaultValue)
    {
        /*
        if (_ldClient == null)
        {
            Console.WriteLine(
                "LD client was never initialized. Return {default} as default value for feature {featureName}",
                defaultValue, featureName);
            {
                return false;
            }
        }

        if (!_ldClient.Initialized)
        {
            Console.WriteLine(
                "LD client's Initialized state is false. Return {default} as default value for feature {featureName}",
                defaultValue, featureName);
            {
                return false;
            }
        }
        */

        return true;
    }
}
