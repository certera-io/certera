using Certes;
using Certes.Acme;
using Certera.Data;
using Certera.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Certera.Web.Controllers
{
    [CertApiKeyAuthorize]
    [Route("api/[controller]")]
    public class CertificateController : Controller
    {
        private readonly DataContext _dataContext;

        public CertificateController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("{name}")]
        public IActionResult Index(string name, string pfxPassword, bool staging = false,
            string format = "pem", bool chain = true)
        {
            if (string.Equals(format, "pfx", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(pfxPassword))
            {
                return BadRequest("pfxPassword must be specified");
            }

            var acmeCert = _dataContext.GetAcmeCertificate(name, staging);

            if (acmeCert == null)
            {
                return NotFound("Certificate with that name does not exist");
            }

            if (acmeCert.LatestValidAcmeOrder == null)
            {
                return NotFound("Certificate does not yet exist");
            }

            // Ensure cert matches the one used during authentication
            var id = User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.Equals(acmeCert.AcmeCertificateId.ToString(), id))
            {
                return StatusCode(403, "Status Code: 403; Forbidden");
            }

            switch (format?.ToLower() ?? "pem")
            {
                case "pfx":
                    var certChain = new CertificateChain(acmeCert.LatestValidAcmeOrder.RawDataPem);
                    var key = KeyFactory.FromPem(acmeCert.Key.RawData);
                    var pfxBuilder = certChain.ToPfx(key);
                    var pfx = pfxBuilder.Build(acmeCert.Subject, pfxPassword);

                    return new ContentResult
                    {
                        Content = Convert.ToBase64String(pfx),
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
                case "pem":
                default:
                    var content = chain ? acmeCert.LatestValidAcmeOrder.RawDataPem :
                        new CertificateChain(acmeCert.LatestValidAcmeOrder.RawDataPem).Certificate.ToPem();
                    return new ContentResult
                    {
                        Content = content,
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
            }
        }
    }
}