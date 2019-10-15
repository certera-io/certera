using Certera.Data;
using Certera.Web.AcmeProviders;
using Certera.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Certera.Web.Middleware
{
    public class SetupAcmeCertMiddleware
    {
        public static readonly string ConsoleHeader = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {
            margin: 10px 0px 0px 10px;
            font-family: Monospace;
            background-color: black;
            color: white;
        }
        a {
            color: white;
        } 
        a:hover {
      	    color: gray;
        }
        a:active {
      	    color: yellow;
        }
        .red {
            color: red;
        }
    </style>
</head>
<body>
    <p>";

        private readonly RequestDelegate _next;

        public SetupAcmeCertMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, 
            ILogger<SetupAcmeCertMiddleware> logger,            
            CertesAcmeProvider certes,
            DataContext dataContext,
            IOptionsSnapshot<HttpServer> httpServerOptions)
        {
            if (httpContext.Request.Method != "POST")
            {
                httpContext.Response.Redirect("/");
                return;
            }

            var host = httpServerOptions.Value.SiteHostname;

            await httpContext.Response.WriteAsync(ConsoleHeader);

            await httpContext.Response.WriteAsync($@"
        Starting certificate acquisition for {host}... <br />");

            var acmeCert = await dataContext.AcmeCertificates
                        .Include(x => x.Key)
                        .Include(x => x.AcmeAccount)
                        .ThenInclude(x => x.Key)                        
                        .FirstAsync(x => x.Subject == host);

            await httpContext.Response.WriteAsync($@"
        Initializing ACME client and ensuring account... <br />");
            certes.Initialize(acmeCert);



            await httpContext.Response.WriteAsync($@"
        Creating order... <br />");
            var acmeOrder = await certes.BeginOrder();
            dataContext.AcmeOrders.Add(acmeOrder);
            dataContext.SaveChanges();



            await httpContext.Response.WriteAsync($@"
        Requesting ACME validation... <br />");
            await certes.Validate();
            dataContext.SaveChanges();



            await httpContext.Response.WriteAsync($@"
        Completing order... (this can take up to 30 seconds)<br />");
            await certes.Complete();



            await httpContext.Response.WriteAsync($@"
        Cleaning up... <br />");
            acmeOrder.AcmeRequests.Clear();
            dataContext.SaveChanges();



            await httpContext.Response.WriteAsync($@"
        Done. Status: {acmeOrder.Status}... <br />");



            bool restart = false;
            if (!acmeOrder.Completed)
            {
                var errors = acmeOrder.Errors.Replace("\r\n", "<br />");
                await httpContext.Response.WriteAsync($@"
        <div class=""red""><p>Errors:<br />{errors}</p></div><br />");
            }
            else
            {
                restart = true;
                await httpContext.Response.WriteAsync(@"
        <br />
        Setup finished successfully! 
        <br />
        <hr />
        <br />
        Certera is restarting...
        <br />
        <script>
            var restartFinished = setInterval(checkLoaded, 5000);
            var tries = 5;
        
            function checkLoaded() {
                var xhttp = new XMLHttpRequest();
                xhttp.onreadystatechange = function() {
                    if (this.readyState == 4 && this.status == 200) {
                        window.location.href=""/"";
                    }
                };
                xhttp.open(""GET"", ""/api/test"", true);
                xhttp.send();
            }
        </script>
        <noscript>
            Please wait 10 seconds for server to restart. Then, <a href=""/"">click here to continue</a>.
        </noscript>");

            }

            await httpContext.Response.WriteAsync(@"
    </p>
</body>
</html>");

            if (restart)
            {
                Program.Restart();
            }
        }
    }
}
