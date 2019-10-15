using Certes;
using Certes.Acme;
using Certera.Data;
using Certera.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Certera.Web.Controllers
{
    [ApiKeyAuthorize]
    [Route("api/[controller]")]
    public class CertificateController : Controller
    {
        private readonly DataContext _dataContext;

        public CertificateController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("{name}")]
        public IActionResult Index(string name, bool staging = false,
            string format = "pem", string pfxPassword = "")
        {
            var acmeCert = _dataContext.GetAcmeCertificate(name, staging);

            if (acmeCert == null)
            {
                return NotFound("Certificate with that name does not exist");
            }

            if (acmeCert.LatestValidAcmeOrder == null)
            {
                return NotFound("Certificate does not yet exist");
            }

            switch (format?.ToLower() ?? "pem")
            {
                case "pfx":
                    var certChain = new CertificateChain(acmeCert.LatestValidAcmeOrder.RawDataPem);
                    var key = KeyFactory.FromPem(acmeCert.Key.RawData);
                    var pfxBuilder = certChain.ToPfx(key);
                    var pfx = pfxBuilder.Build(acmeCert.Subject, pfxPassword ?? string.Empty);

                    return new ContentResult
                    {
                        Content = Convert.ToBase64String(pfx),
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
                case "pem":
                default:
                    return new ContentResult
                    {
                        Content = acmeCert.LatestValidAcmeOrder.RawDataPem,
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
            }
        }
    }
}