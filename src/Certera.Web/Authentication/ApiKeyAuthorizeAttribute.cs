using Microsoft.AspNetCore.Authorization;

namespace Certera.Web.Authentication
{
    public class CertApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public CertApiKeyAuthorizeAttribute()
        {
            AuthenticationSchemes = CertApiKeyAuthenticationHandler.AuthScheme;
        }
    }

    public class KeyApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public KeyApiKeyAuthorizeAttribute()
        {
            AuthenticationSchemes = KeyApiKeyAuthenticationHandler.AuthScheme;
        }
    }
}
