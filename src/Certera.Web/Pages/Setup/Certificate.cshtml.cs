using Certera.Web.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Certera.Web.Pages.Setup
{
    public class CertificateModel : PageModel
    {
        private readonly IOptionsSnapshot<HttpServer> _httpServerOptions;

        public string HttpsHost { get; set; }

        public CertificateModel(IOptionsSnapshot<HttpServer> httpServerOptions)
        {
            _httpServerOptions = httpServerOptions;
        }

        public void OnGet()
        {
            HttpsHost = _httpServerOptions.Value.SiteHostname;
        }
    }
}