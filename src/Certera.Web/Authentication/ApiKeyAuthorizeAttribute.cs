using Microsoft.AspNetCore.Authorization;

namespace Certera.Web.Authentication
{
    public class ApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public ApiKeyAuthorizeAttribute()
        {
            AuthenticationSchemes = ApiKeyAuthenticationHandler.AuthScheme;
        }
    }
}
