using System.Text.RegularExpressions;
using S3WebApi.Helpers;
using Serilog;
using ILogger = Serilog.ILogger;

namespace S3WebApi.DMSAuth
{
    public class AuthSecret
    {
        public string Username { get; set; }

        public string Password { get; set; }
        public Dictionary<string, string> Certificates { get; set; }
        public Dictionary<string, string> Mapping { get; set; }

        private readonly object _mapLock = new();
        private int _currentMap;

        private ILogger Logger => Log.ForContext<AuthSecret>();

        /// <summary>
        /// This build the mapping between AppId and the certificate.
        /// It is based on convention regarding the structure of the Vault Secret
        /// </summary>
        /// <param name="selector"></param>
        public void BuildMapping(string selector)
        {
            Mapping = new Dictionary<string, string>();
            foreach (var item in Certificates)
            {
                if (item.Key.StartsWith(selector, true, null))
                {
                    var split = item.Value.Split(':');
                    if (split.Length == 3)
                    {
                        Mapping.Add(split[1], split[2]);
                    }
                }
            }
        }

        /// <summary>
        /// Round Robin of the certs stored in Mapping
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<string, string> NextMap()
        {
            lock (_mapLock)
            {
                if (Mapping != null && Mapping.Count > 0)
                {
                    _currentMap++;
                    if (_currentMap >= Mapping.Count)
                    {
                        _currentMap = 0;
                    }

                    return Mapping.ElementAt(_currentMap);
                }

                return new KeyValuePair<string, string>();
            }
        }

        public KeyValuePair<string, string> GetDesignatedCert(IConfiguration configuration)
        {
            configuration.RequireNotNull(nameof(configuration));

            var certSelector = configuration["AppPrincipal:CertSelector"];

            // Pod name is expected to be in the format of "mshare-integration-marsh-app-web-api-1"
            var podName = Environment.GetEnvironmentVariable("POD_NAME");
            if (string.IsNullOrEmpty(podName))
            {

                return GetDefaultCert(configuration);
            }

            const string podIndexPattern = @"-(\d+)$";
            var match = Regex.Match(podName, podIndexPattern);
            if (!match.Success)
            {

                return GetDefaultCert(configuration);
            }

            int podId = Convert.ToInt32(match.Groups[1].Value);
            string spnIndex = configuration[$"AppPrincipal:PodSpnMapping:{podId}"];
            if (string.IsNullOrEmpty(spnIndex))
            {

                return GetDefaultCert(configuration);
            }

            string spnSelector = $"{certSelector}{spnIndex}";

            if (!Certificates.TryGetValue(spnSelector, out var spnKeyValue))
            {

                return GetDefaultCert(configuration);
            }

            var spnValueSplits = spnKeyValue.Split(':');
            return new KeyValuePair<string, string>(spnValueSplits[1], spnValueSplits[2]);
        }

        private KeyValuePair<string, string> GetDefaultCert(IConfiguration configuration)
        {
            var defaultAccountSelector = $"{configuration["AppPrincipal:CertSelector"]}{configuration["AppPrincipal:PodSpnMapping:default"]}";

            if (!Certificates.TryGetValue(defaultAccountSelector, out var defaultSpnKeyValue))
            {
                var fallbackAccount = $"{configuration["AppPrincipal:CertSelector"]}1";



                if (!Certificates.TryGetValue(fallbackAccount, out var fallbackSpnKeyValue))
                {
                    var error = $"The fallback account {fallbackAccount} does not exist";
                    //Logger.Log().Error(error);
                    Console.WriteLine(error);
                    throw new ApplicationException(error);
                }

                var fallbackValueSplits = fallbackSpnKeyValue.Split(':');
                return new KeyValuePair<string, string>(fallbackValueSplits[1], fallbackValueSplits[2]);
            }
            var defaultValueSplits = defaultSpnKeyValue.Split(':');
            return new KeyValuePair<string, string>(defaultValueSplits[1], defaultValueSplits[2]);
        }
    }
}
