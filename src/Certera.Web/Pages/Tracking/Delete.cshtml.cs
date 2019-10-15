using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certera.Data;
using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Certera.Web.Pages.Tracking
{
    public class DeleteModel : PageModel
    {
        private readonly DataContext _dataContext;

        public DeleteModel(DataContext dataContext)
        {
            _dataContext = dataContext;
        }


        [BindProperty]
        public DomainCertificate DomainCertificate { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            DomainCertificate = await _dataContext.DomainCertificates.FindAsync(id);
            if (DomainCertificate == null)
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

            DomainCertificate = await _dataContext.DomainCertificates.FindAsync(id);
            if (DomainCertificate == null)
            {
                return NotFound();
            }

            _dataContext.DomainCertificates.Remove(DomainCertificate);
            await _dataContext.SaveChangesAsync();

            StatusMessage = "Certificate deleted";

            return RedirectToPage("./Index");
        }
    }
}