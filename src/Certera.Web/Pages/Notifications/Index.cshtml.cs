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
using System.Security.Claims;

namespace Certera.Web.Pages.Notifications
{
    public class IndexModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public IndexModel(Certera.Data.DataContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public NotificationSetting NotificationSetting { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            NotificationSetting = await _context.NotificationSettings
                .Include(x => x.ApplicationUser)
                .FirstOrDefaultAsync(m => m.ApplicationUserId == userId);

            if (NotificationSetting == null)
            {
                NotificationSetting = new NotificationSetting
                {
                    ApplicationUserId = userId
                };
                _context.NotificationSettings.Add(NotificationSetting);
                await _context.SaveChangesAsync();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(NotificationSetting).State = EntityState.Modified;

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            NotificationSetting.ApplicationUserId = userId;

            try
            {
                await _context.SaveChangesAsync();
                StatusMessage = "Notification settings updated";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationSettingExists(NotificationSetting.NotificationSettingId))
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

        private bool NotificationSettingExists(long id)
        {
            return _context.NotificationSettings.Any(e => e.NotificationSettingId == id);
        }
    }
}
