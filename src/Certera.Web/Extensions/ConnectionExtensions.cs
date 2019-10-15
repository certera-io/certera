using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Certera.Web.Extensions
{
    // adapted from http://stackoverflow.com/a/41242493
    public static class ConnectionExtensions
    {
        public const string NullIPv6 = "::1";

        public static bool IsLocal(this ConnectionInfo conn)
        {
            if (!conn.RemoteIpAddress.IsSet())
            {
                return true;
            }

            // we have a remote address set up
            // is local is same as remote, then we are local
            if (conn.LocalIpAddress.IsSet())
            {
                return conn.RemoteIpAddress.Equals(conn.LocalIpAddress);
            }

            // else we are remote if the remote IP address is not a loopback address
            return conn.RemoteIpAddress.IsLoopback();
        }

        public static bool IsLocal(this ConnectionContext ctx)
        {
            var remoteIp = (ctx.RemoteEndPoint as IPEndPoint)?.Address;
            if (remoteIp == null || !remoteIp.IsSet())
            {
                return true;
            }

            var localIp = (ctx.LocalEndPoint as IPEndPoint)?.Address;
            if (localIp != null && localIp.IsSet())
            {
                return remoteIp.Equals(localIp);
            }

            return remoteIp.IsLoopback();
        }

        public static bool IsLocal(this HttpContext ctx)
        {
            return ctx.Connection.IsLocal();
        }

        public static bool IsLocal(this HttpRequest req)
        {
            return req.HttpContext.IsLocal();
        }

        public static bool IsSet(this IPAddress address)
        {
            return address != null && address.ToString() != NullIPv6;
        }

        public static bool IsLoopback(this IPAddress address)
        {
            return IPAddress.IsLoopback(address);
        }
    }
}
