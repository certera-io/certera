using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Certera.Data;
using Certera.Data.Models;
using Microsoft.Extensions.Options;
using Certera.Web.Options;

namespace Certera.Web.Pages.Certificates
{
    public class EditModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;
        private readonly IOptionsSnapshot<HttpServer> _httpServerOptions;

        public EditModel(Certera.Data.DataContext context, IOptionsSnapshot<HttpServer> httpServerOptions)
        {
            _context = context;
            _httpServerOptions = httpServerOptions;
        }

        [BindProperty]
        public AcmeCertificate AcmeCertificate { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AcmeCertificate = await _context.AcmeCertificates
                .Include(a => a.AcmeAccount)
                .Include(a => a.Key)
                .FirstOrDefaultAsync(m => m.AcmeCertificateId == id);

            if (AcmeCertificate == null)
            {
                return NotFound();
            }

            ViewData["AcmeAccountId"] = new SelectList(
                _context.AcmeAccounts.Select(x => new
                {
                    Id = x.AcmeAccountId,
                    Name = x.AcmeContactEmail + (x.IsAcmeStaging ? " (staging)" : string.Empty)
                })
                , "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!AcmeCertificateExists(AcmeCertificate.AcmeCertificateId))
            {
                return NotFound();
            }

            var cert = await _context.AcmeCertificates
                .FirstOrDefaultAsync(x => x.AcmeCertificateId == AcmeCertificate.AcmeCertificateId);

            // Prevent changing the subject for this site's certificate
            if (cert.Subject == _httpServerOptions.Value.SiteHostname &&
                !string.Equals(AcmeCertificate.Subject, cert.Subject, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Cannot change site certificate subject");
                return Page();
            }

            cert.CsrCommonName = AcmeCertificate.CsrCommonName;
            cert.CsrCountryName = AcmeCertificate.CsrCountryName;
            cert.CsrLocality = AcmeCertificate.CsrLocality;
            cert.CsrOrganization = AcmeCertificate.CsrOrganization;
            cert.CsrOrganizationUnit = AcmeCertificate.CsrOrganizationUnit;
            cert.CsrState = AcmeCertificate.CsrState;
            cert.CsrState = AcmeCertificate.CsrState;
            cert.Name = AcmeCertificate.Name;
            cert.KeyId = AcmeCertificate.KeyId;
            cert.Subject = AcmeCertificate.Subject;
            cert.SANs = AcmeCertificate.SANs;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AcmeCertificateExists(AcmeCertificate.AcmeCertificateId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AcmeCertificateExists(long id)
        {
            return _context.AcmeCertificates.Any(e => e.AcmeCertificateId == id);
        }
    }
}
