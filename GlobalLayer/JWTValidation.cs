using Microsoft.IdentityModel.Tokens;
using S3WebApi.DMSAuth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Text;

namespace S3WebApi.GlobalLayer
{
    public class JWTValidation
    {
        private readonly IConfiguration _configuration;
        private readonly AuthSecret _authSecret;
        private readonly string _configKeyName;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly bool _validateLifetime;
        private readonly bool _validateAudience;
        private readonly bool _validateIssuer;

        private string _privateKey = string.Empty;

        public JWTValidation(IConfiguration configuration, AuthSecret authSecret)
        {
            _configuration = configuration;
            _authSecret = authSecret;
            _configKeyName = _configuration[ConfigurationPaths.HeaderValidation_HC_KeyNme];
            _issuer = _configuration[ConfigurationPaths.HeaderValidation_Issuer];
            _audience = _configuration[ConfigurationPaths.HeaderValidation_Audience];
            _validateLifetime = Convert.ToBoolean(_configuration[ConfigurationPaths.HeaderValidation_ValidateLifetime]);
            _validateAudience = Convert.ToBoolean(_configuration[ConfigurationPaths.HeaderValidation_ValidateAudience]);
            _validateIssuer = Convert.ToBoolean(_configuration[ConfigurationPaths.HeaderValidation_ValidateIssuer]);
        }
        public bool ValidateToken(string authToken)
        {
            try
            {
                _privateKey = _authSecret.Certificates[_configKeyName];
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();
                IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateLifetime = _validateLifetime,
                ValidateAudience = _validateAudience,
                ValidateIssuer = _validateIssuer,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_privateKey)) // The same key as the one that generate the token
            };
        }
    }
}
