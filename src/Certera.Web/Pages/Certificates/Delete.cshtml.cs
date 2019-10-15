using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Certera.Data;
using Certera.Data.Models;
using Microsoft.Extensions.Options;
using Certera.Web.Options;

namespace Certera.Web.Pages.Certificates
{
    public class DeleteModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;
        private readonly IOptionsSnapshot<HttpServer> _httpServerOptions;

        public DeleteModel(Certera.Data.DataContext context, IOptionsSnapshot<HttpServer> httpServerOptions)
        {
            _context = context;
            _httpServerOptions = httpServerOptions;
        }

        [BindProperty]
        public AcmeCertificate AcmeCertificate { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
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
            // Prevent deleting of site certificate
            if (AcmeCertificate.Subject == _httpServerOptions.Value.SiteHostname)
            {
                StatusMessage = "Cannot delete site certificate";
                return Page();
            }

            _context.AcmeCertificates.Remove(AcmeCertificate);
            await _context.SaveChangesAsync();

            StatusMessage = "Certificate deleted";

            return RedirectToPage("./Index");
        }
    }
}
