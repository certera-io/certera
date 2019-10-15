using Certera.Web.Extensions;
using Certera.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Certera.Web.Middleware
{
    public class AllowedRemoteIPMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AllowedRemoteIPMiddleware> _logger;
        private readonly IOptionsMonitor<AllowedRemoteIPAddresses> _allowedIPs;

        public AllowedRemoteIPMiddleware(
            RequestDelegate next,
            ILogger<AllowedRemoteIPMiddleware> logger,
            IOptionsMonitor<AllowedRemoteIPAddresses> allowedIPs)
        {
            _allowedIPs = allowedIPs;
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Always allow local connections. 
            // Also allow access to the acme-challenge endpoint since Let's Encrypt
            // does not publish their IP addresses.
            if (context.Request.IsLocal() ||
                context.Request.Path.StartsWithSegments("/.well-known/acme-challenge"))
            {
                await _next.Invoke(context);
                return;
            }

            bool isApi = context.Request.Path.StartsWithSegments("/api");

            string ipList = isApi
                ? _allowedIPs.CurrentValue.API
                : _allowedIPs.CurrentValue.AdminUI;

            // Allow if value is "wildcard" (denoting any IP)
            if (string.Equals("*", ipList))
            {
                await _next.Invoke(context);
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress;

            var ips = ipList.Split(',',';', StringSplitOptions.RemoveEmptyEntries);

            var bytes = remoteIp.GetAddressBytes();
            var isBadIP = true;

            foreach (var address in ips)
            {
                var testIp = IPAddress.Parse(address);
                if (testIp.GetAddressBytes().SequenceEqual(bytes))
                {
                    isBadIP = false;
                    break;
                }
            }

            if (isBadIP)
            {
                _logger.LogInformation($"Forbidden request from remote IP address: {remoteIp}. Does not match {ipList}");
                context.Response.StatusCode = 401;
                return;
            }

            await _next.Invoke(context);
        }
    }
}
