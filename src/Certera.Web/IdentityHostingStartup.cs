using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Certera.Web.Areas.Identity.IdentityHostingStartup))]
namespace Certera.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {});
        }
    }
}