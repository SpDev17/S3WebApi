using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using S3WebApi.DMSAuth;
using S3WebApi.GlobalLayer;

namespace S3WebApi.Filters
{
    public class MShareAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext httpContext)
        {
            if (Debugger.IsAttached)
            {
                return;
            }

            // When integrating apps call our Apigee endpoint:
            // - they need to pass a valid access token (acquired from Core API Access Management API)
            // - our Apigee endpoint will validate the access token
            // - on successful validation, Apigee will then pass a signed JWT to our backend API 
            // - we then need to validate this JWT token to ensure that the request has gone through
            //   full authentication flow via Apigee, i.e. this will prevent apps from being able to call
            //   the backend API directly.

            if (!httpContext.HttpContext.Request.Headers.TryGetValue("JWT", out var jwtToken))
            {
                httpContext.Result = new UnauthorizedResult();
                return;
            }

            var authSecret = httpContext.HttpContext.RequestServices.GetRequiredService<AuthSecret>();
            var configuration = httpContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            try
            {
                var jwtValidation = new JWTValidation(configuration, authSecret);
                if (!jwtValidation.ValidateToken(jwtToken))
                {
                    httpContext.Result = new UnauthorizedResult();
                }
            }
            catch
            {
                httpContext.Result = new UnauthorizedResult();
            }
        }
    }
}
