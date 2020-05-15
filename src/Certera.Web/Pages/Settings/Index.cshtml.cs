using Certera.Core.Notifications;
using Certera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly DataContext _dataContext;
        private readonly MailSender _mailSender;
        private readonly IOptionsSnapshot<MailSenderInfo> _senderInfo;

        public IndexModel(DataContext dataContext, MailSender mailSender, IOptionsSnapshot<MailSenderInfo> senderInfo)
        {
            _dataContext = dataContext;
            _mailSender = mailSender;
            _senderInfo = senderInfo;
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

        [BindProperty]
        public string Recipients { get; set; }

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

        public IActionResult OnPostSendTestEmail()
        {
            if (string.IsNullOrWhiteSpace(_senderInfo?.Value?.Host))
            {
                StatusMessage = "No SMTP configuration specified";
            }
            else
            {
                _mailSender.Initialize(_senderInfo.Value);
                var recipients = new List<string>();
                if (!string.IsNullOrWhiteSpace(Recipients))
                {
                    recipients.AddRange(Recipients
                        .Split(',', ';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()));
                    _mailSender.Send("[certera] Test Email", "Test email from Certera", recipients.ToArray());
                    StatusMessage = "Test email sent";
                }
                else
                {
                    StatusMessage = "No recipient specified";
                }
            }

            return Page();
        }
    }
}