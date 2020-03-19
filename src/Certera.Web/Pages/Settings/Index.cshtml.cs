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

        [BindProperty]
        public string DnsScriptEnvironmentVariables { get; set; }
        [BindProperty]
        public string SetScript { get; set; }
        [BindProperty]
        public string SetScriptArguments { get; set; }
        [BindProperty]
        public string CleanupScript { get; set; }
        [BindProperty]
        public string CleanupScriptArguments { get; set; }

        public string StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            RenewCertificateDays = _dataContext.GetSetting(Data.Settings.RenewCertificateDays, 30);
            DnsScriptEnvironmentVariables = _dataContext.GetSetting<string>(Data.Settings.Dns01SetEnvironmentVariables, null);
            SetScript = _dataContext.GetSetting<string>(Data.Settings.Dns01SetScript, null);
            CleanupScript = _dataContext.GetSetting<string>(Data.Settings.Dns01CleanupScript, null);
            SetScriptArguments = _dataContext.GetSetting<string>(Data.Settings.Dns01SetScriptArguments, null);
            CleanupScriptArguments = _dataContext.GetSetting<string>(Data.Settings.Dns01CleanupScriptArguments, null);

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _dataContext.SetSetting(Data.Settings.RenewCertificateDays, RenewCertificateDays);
            _dataContext.SetSetting(Data.Settings.Dns01SetEnvironmentVariables, DnsScriptEnvironmentVariables);
            _dataContext.SetSetting(Data.Settings.Dns01SetScript, SetScript);
            _dataContext.SetSetting(Data.Settings.Dns01CleanupScript, CleanupScript);
            _dataContext.SetSetting(Data.Settings.Dns01SetScriptArguments, SetScriptArguments);
            _dataContext.SetSetting(Data.Settings.Dns01CleanupScriptArguments, CleanupScriptArguments);

            StatusMessage = "Settings saved";

            return Page();
        }
    }
}