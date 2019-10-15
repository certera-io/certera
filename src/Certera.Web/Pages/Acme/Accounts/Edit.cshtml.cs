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

namespace Certera.Web.Pages.Acme.Accounts
{
    public class EditModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public EditModel(Certera.Data.DataContext context)
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
           ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(AcmeAccount).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                StatusMessage = "Account updated";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AcmeAccountExists(AcmeAccount.AcmeAccountId))
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

        private bool AcmeAccountExists(long id)
        {
            return _context.AcmeAccounts.Any(e => e.AcmeAccountId == id);
        }
    }
}
