using Certera.Data;
using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Keys
{
    public class EditModel : PageModel
    {
        private readonly DataContext _context;

        public EditModel(DataContext context)
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var key = await _context.Keys.FirstOrDefaultAsync(m => m.KeyId == Key.KeyId);

            if (key == null)
            {
                return NotFound();
            }

            key.Name = Key.Name;
            key.Description = Key.Description;
            key.DateRotationReminder = Key.DateRotationReminder;

            try
            {
                await _context.SaveChangesAsync();
                StatusMessage = "Key updated";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KeyExists(Key.KeyId))
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

        private bool KeyExists(long id)
        {
            return _context.Keys.Any(e => e.KeyId == id);
        }
    }
}
