using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Certera.Data;
using Certera.Data.Models;

namespace Certera.Web.Pages.Keys
{
    public class DeleteModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public DeleteModel(Certera.Data.DataContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Key Key { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Key = await _context.Keys.FirstOrDefaultAsync(m => m.KeyId == id);

            if (Key == null)
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

            Key = await _context.Keys.FindAsync(id);

            if (Key != null)
            {
                _context.Keys.Remove(Key);
                try
                {
                    await _context.SaveChangesAsync();
                    StatusMessage = "Key deleted";
                }
                catch (DbUpdateException)
                {
                    StatusMessage = "Unable to delete key in use";
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
