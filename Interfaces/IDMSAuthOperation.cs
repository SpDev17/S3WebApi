using Azure.Identity;
using PnP.Core.Auth;
using Microsoft.SharePoint.Client;

namespace S3WebApi.Interfaces
{
    public interface IDMSAuthOperation
    {
        /// <summary>
        /// Get the ClientContext for a particular Site/WebApp Url
        /// </summary>
        /// <param name="url">The WebApp/Site URL used for creating the client context.</param>
        /// <param name="userID">The UserID this context is associated with - used for caching to ensure each user get's their own context.
        /// If null or empty, the context will not be cached.</param>
        /// <returns>A <see cref="ClientContext"/> for using to interact with the given site.</returns>
        public ClientContext getUserToken(string url, string userID, bool useCache = true, string spnName = null);
        public ClientContext getTransientUserToken(string url, string userID, string spnName = null);
        public ClientContext getUserTokenForEventRegistration(string url);
        public X509CertificateAuthenticationProvider GetCertificateAuthenticationProvider(string url, string spnName = null);
        public ClientCertificateCredential GetClientCertificateCredential(string spnName = null);

        public Task<string> AcquireTokenWithCertificateAsync(string spnName = null, string scope = "");
    }
}
