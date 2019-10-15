using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Acme.Accounts
{
    public class DeleteModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public DeleteModel(Certera.Data.DataContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AcmeAccount AcmeAccount { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AcmeAccount = await _context.AcmeAccounts
                .Include(a => a.ApplicationUser)
                .Include(a => a.Key).FirstOrDefaultAsync(m => m.AcmeAccountId == id);

            if (AcmeAccount == null)
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
            
            AcmeAccount = await _context.AcmeAccounts
                .Include(a => a.ApplicationUser)
                .Include(a => a.Key).FirstOrDefaultAsync(m => m.AcmeAccountId == id);

            if (AcmeAccount != null)
            {
                _context.AcmeAccounts.Remove(AcmeAccount);
                try
                {
                    await _context.SaveChangesAsync();
                    StatusMessage = "ACME account deleted";
                }
                catch (DbUpdateException)
                {
                    StatusMessage = "Unable to delete ACME account in use";
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
