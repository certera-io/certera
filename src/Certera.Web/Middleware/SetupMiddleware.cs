using Certera.Data;
using Certera.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Middleware
{
    public class SetupMiddleware
    {
        private readonly RequestDelegate _next;

        public SetupMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext,
            IWritableOptions<Setup> setupOptions,
            IOptionsSnapshot<HttpServer> httpServerOptions,
            DataContext dataContext)
        {
            if (IsSafeEndpoint(httpContext))
            {
                await _next.Invoke(httpContext);
                return;
            }

            var setupRequested = httpContext.Request.Path.StartsWithSegments("/setup");

            if (setupRequested && setupOptions.Value.Finished)
            {
                // Setup is done, go to /
                httpContext.Response.Redirect("/");
                return;
            }

            string page = httpContext.Request.Path;

            if (!setupOptions.Value.Finished)
            {
                if (!CompletedAccountStep(dataContext))
                {
                    page = "/setup";
                }
                else if (!CompletedAcmeStep(dataContext))
                {
                    page = "/setup/acme";
                }
                else if (!CompletedServerSetup(dataContext, httpServerOptions.Value))
                {
                    page = "/setup/server";
                }
                else if (!CompletedCertificateStep(dataContext, httpServerOptions.Value))
                {
                    page = "/setup/certificate";
                }
                else
                {
                    page = "/setup/finished";
                }
            }

            if (httpContext.Request.Method != "POST" &&
                page != httpContext.Request.Path)
            {
                httpContext.Response.Redirect(page);
                return;
            }

            await _next(httpContext);
        }

        private bool IsSafeEndpoint(HttpContext httpContext)
        {
            return httpContext.Request.Path.StartsWithSegments("/.well-known") ||
                   httpContext.Request.Path.StartsWithSegments("/api/test");
        }

        /// <summary>
        /// This setup hook is available for startup to determine whether setup has finished or not.
        /// It's a quick way to update the flag so that all of the checks don't need to happen on every request.
        /// </summary>
        public static bool SetupFinished(DataContext dataContext, HttpServer httpServerOptions)
        {
            return CompletedAccountStep(dataContext) &&
                CompletedAcmeStep(dataContext) &&
                CompletedServerSetup(dataContext, httpServerOptions) &&
                CompletedCertificateStep(dataContext, httpServerOptions);
        }

        private static bool CompletedAccountStep(DataContext dataContext)
        {
            return dataContext.ApplicationUsers.Any();
        }

        private static bool CompletedAcmeStep(DataContext dataContext)
        {
            return dataContext.AcmeAccounts.Count() > 1 && dataContext.Keys.Any();
        }

        private static bool CompletedServerSetup(DataContext dataContext, HttpServer httpServerOptions)
        {
            return !string.IsNullOrWhiteSpace(httpServerOptions.SiteHostname) && dataContext.AcmeCertificates.Any();
        }

        private static bool CompletedCertificateStep(DataContext dataContext, HttpServer httpServerOptions)
        {
            var cert = dataContext.GetAcmeCertificate(httpServerOptions.SiteHostname);

            if (cert == null || cert.LatestValidAcmeOrder == null || !cert.LatestValidAcmeOrder.Completed)
            {
                return false;
            }

            // Correct certificate obtained successfully
            return true;
        }
    }
}
