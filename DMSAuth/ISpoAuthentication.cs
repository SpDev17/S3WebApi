using Azure.Identity;
using Microsoft.SharePoint.Client;
using PnP.Core.Auth;

namespace S3WebApi.DMSAuth
{
    public interface ISpoAuthentication
    {
        public ClientContext GetUserToken(string url, bool useCache = true);
        public ClientContext GetTransientUserToken(string url);
        public ClientContext GetUserTokenForEventRegistration(string url);
        public X509CertificateAuthenticationProvider GetCertificateAuthenticationProvider();
        public ClientCertificateCredential GetClientCertificateCredential();
    }
}
