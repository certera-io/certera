using Certera.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Certera.Web.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthScheme = "ApiKey";
        private readonly DataContext _dataContext;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, DataContext dataContext,
            SignInManager<ApplicationUser> signInManager) 
            : base(options, logger, encoder, clock)
        {
            _dataContext = dataContext;
            _signInManager = signInManager;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("apikey"))
            {
                return AuthenticateResult.Fail("Missing header: apikey");
            }

            ApplicationUser user = null;
            var apiKey = Request.Headers["apikey"][0];
            user = await _dataContext.ApplicationUsers.FirstOrDefaultAsync(x => x.ApiKey1 == apiKey || x.ApiKey2 == apiKey);

            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid apikey");
            }

            // Creates a principal from user with all of the same claims (including role claim)
            // as the one from signing in via email & pwd.
            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            var ticket = new AuthenticationTicket(principal, AuthScheme);

            return AuthenticateResult.Success(ticket);
        }
    }
}
