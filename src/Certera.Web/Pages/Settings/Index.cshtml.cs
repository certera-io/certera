using Certera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Certera.Web.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly DataContext _dataContext;

        public IndexModel(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Range(10, 45, ErrorMessage = "Must be between 10 and 45 days")]
        [BindProperty]
        public int RenewCertificateDays { get; set; }

        public string StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            RenewCertificateDays = _dataContext.GetSetting(Data.Settings.RenewCertificateDays, 30);

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _dataContext.SetSetting(Data.Settings.RenewCertificateDays, RenewCertificateDays);
            StatusMessage = "Settings saved";

            return Page();
        }
    }
}