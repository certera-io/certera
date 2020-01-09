using Certera.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Certera.Web.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(DataContext dataContext, UserManager<ApplicationUser> userManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
        }

        public string StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }
    }
}