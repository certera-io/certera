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



        public async Task<IActionResult> OnPostRotateKeyAsync(long id, string key)
        {
            // To not get validation errors due to other post handler
            ModelState.Clear();

            Key = await _context.Keys.FirstOrDefaultAsync(m => m.KeyId == id);

            if (Key == null)
            {
                return NotFound();
            }

            switch (key)
            {
                case "apikey1":
                    Key.ApiKey1 = ApiKeyGenerator.CreateApiKey();
                    break;
                case "apikey2":
                    Key.ApiKey2 = ApiKeyGenerator.CreateApiKey();
                    break;
            }

            await _context.SaveChangesAsync();

            return Page();
        }
        
        private bool KeyExists(long id)
        {
            return _context.Keys.Any(e => e.KeyId == id);
        }
    }
}
