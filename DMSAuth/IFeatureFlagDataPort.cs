namespace S3WebApi.DMSAuth
{
    public interface IFeatureFlagDataPort
    {
        bool IsFeatureEnabled(
            string featureName,
            bool defaultValue = false,
            string originatingSystemCode = null,
            string siteCollectionUrl = null);

        string? GetFeatureValue(string featureName, string? defaultValue = null);
    }
}
