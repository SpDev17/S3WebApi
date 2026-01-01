using System.Text.RegularExpressions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace S3WebApi.Helpers
{
    public class UserAgentDecorationHelper
    {
        private readonly IConfiguration _configuration;
        private ILogger Logger => Log.ForContext<UserAgentDecorationHelper>();

        public UserAgentDecorationHelper(IConfiguration configuration)
        {
            configuration.RequireNotNull(nameof(configuration));
            _configuration = configuration;
        }

        public string GetDecoration()
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(envName))
            {
                //Logger.Log().Warning("ASPNETCORE_ENVIRONMENT environment variable is null");
                return _configuration["DynamicConfigurations:DefaultUserAgent"];
            }

            var region = Environment.GetEnvironmentVariable("REGION");
            if (string.IsNullOrEmpty(region))
            {
                //Logger.Log().Warning("REGION environment variable is null");
                return _configuration["DynamicConfigurations:DefaultUserAgent"];
            }

            var podName = Environment.GetEnvironmentVariable("POD_NAME");
            if (string.IsNullOrEmpty(podName))
            {
                //Logger.Log().Warning("POD_NAME environment variable is null");
                return _configuration["DynamicConfigurations:DefaultUserAgent"];
            }

            const string podIndexPattern = @"-(\d+)$";
            var match = Regex.Match(podName, podIndexPattern);
            if (!match.Success)
            {
                //Logger.Log().Warning("Invalid POD_NAME {podName}", podName);
                return _configuration["DynamicConfigurations:DefaultUserAgent"];
            }

            int podId = Convert.ToInt32(match.Groups[1].Value);
            var userAgent = string.Format(_configuration["DynamicConfigurations:UserAgentFormat"],
                region.ToLower(),
                envName.ToLower(),
                podId);

            return userAgent;
        }
    }
}
