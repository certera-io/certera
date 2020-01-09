using Certera.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Certera.Web.Authentication
{
    public class CertApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthScheme = "CertApiKey";
        private readonly DataContext _dataContext;

        public CertApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, DataContext dataContext) 
            : base(options, logger, encoder, clock)
        {
            _dataContext = dataContext;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("apikey"))
            {
                return AuthenticateResult.Fail("Missing header: apikey");
            }

            var apiKey = Request.Headers["apikey"][0];
            var cert = await _dataContext.AcmeCertificates.FirstOrDefaultAsync(x => x.ApiKey1 == apiKey || x.ApiKey2 == apiKey);

            if (cert == null)
            {
                return AuthenticateResult.Fail("Invalid apikey");
            }

            var identity = new ClaimsIdentity("CustomApiKey");
            identity.AddClaim(new Claim(ClaimTypes.Name, "cert"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, cert.AcmeCertificateId.ToString()));
            
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, AuthScheme);

            return AuthenticateResult.Success(ticket);
        }
    }
}
