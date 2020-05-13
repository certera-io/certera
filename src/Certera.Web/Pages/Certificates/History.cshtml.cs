using Certera.Core.Helpers;
using Certera.Data;
using Certera.Data.Models;
using Certera.Web.AcmeProviders;
using Certes.Acme.Resource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Certificates
{
    public class HistoryModel : PageModel
    {
        private readonly DataContext _context;
        private readonly CertesAcmeProvider _certesAcmeProvider;
        private readonly ILogger<HistoryModel> _logger;

        public HistoryModel(DataContext context, CertesAcmeProvider certesAcmeProvider, ILogger<HistoryModel> logger)
        {
            _context = context;
            _certesAcmeProvider = certesAcmeProvider;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public AcmeCertificate AcmeCertificate { get; set; }

        public string OcspStatus { get; set; }

        [BindProperty]
        public string RevocationReason { get; set; }

        public async Task<IActionResult> OnGet(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AcmeCertificate = await _context.AcmeCertificates
                .Include(a => a.AcmeOrders)
                .ThenInclude(o => o.DomainCertificate)
                .FirstOrDefaultAsync(m => m.AcmeCertificateId == id);

            if (AcmeCertificate == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id, string action, string key)
        {
            AcmeCertificate = await _context.AcmeCertificates
                .Include(a => a.AcmeAccount)
                .ThenInclude(a => a.Key)
                .Include(a => a.AcmeOrders)
                .ThenInclude(o => o.DomainCertificate)
                .FirstOrDefaultAsync(m => m.AcmeCertificateId == id);

            if (AcmeCertificate == null)
            {
                return NotFound();
            }

            switch(action.ToLower())
            {
                case "keychange":
                    switch (key)
                    {
                        case "apikey1":
                            AcmeCertificate.ApiKey1 = ApiKeyGenerator.CreateApiKey();
                            break;
                        case "apikey2":
                            AcmeCertificate.ApiKey2 = ApiKeyGenerator.CreateApiKey();
                            break;
                    }

                    await _context.SaveChangesAsync();
                    break;
                case "ocspcheck":
                    try
                    {
                        var order = AcmeCertificate.GetLatestValidAcmeOrder();
                        if (order?.Certificate != null)
                        {
                            var client = new OcspClient();
                            var status = client.GetOcspStatus(order.Certificate);
                            OcspStatus = status.ToString();
                        }
                        else
                        {
                            OcspStatus = "No certificate";
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning($"Error obtaining OCSP status:{e.Message}");
                        OcspStatus = "Error";
                    }
                    break;
                case "revoke":
                    {
                        var order = AcmeCertificate.GetLatestValidAcmeOrder();
                        if (order?.RawDataPem != null)
                        {
                            _certesAcmeProvider.Initialize(AcmeCertificate);

                            var cert = new Certes.Acme.CertificateChain(order.RawDataPem);
                            var reason = (RevocationReason)Enum.Parse(typeof(RevocationReason), RevocationReason, true);
                            await _certesAcmeProvider.Revoke(cert.Certificate.ToDer(), reason);
                            StatusMessage = "Certificate revocation submitted";
                        }
                        break;
                    }
            }

            return Page();
        }
    }
}