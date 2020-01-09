using Certera.Data;
using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Certificates
{
    public class HistoryModel : PageModel
    {
        private readonly DataContext _context;

        public HistoryModel(DataContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public AcmeCertificate AcmeCertificate { get; set; }

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

        public async Task<IActionResult> OnPostAsync(long id, string key)
        {
            AcmeCertificate = await _context.AcmeCertificates
                .Include(a => a.AcmeOrders)
                .ThenInclude(o => o.DomainCertificate)
                .FirstOrDefaultAsync(m => m.AcmeCertificateId == id);

            if (AcmeCertificate == null)
            {
                return NotFound();
            }

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

            return Page();
        }
    }
}