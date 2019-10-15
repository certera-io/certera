using Certera.Data;
using Certera.Web.Options;
using Certera.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Setup
{
    public class ServerModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly DataContext _dataContext;
        private readonly IWritableOptions<HttpServer> _httpServerOptions;
        private readonly KeyGenerator _keyGenerator;

        public ServerModel(ILogger<IndexModel> logger,
            DataContext dataContext,
            IWritableOptions<HttpServer> httpServerOptions,
            KeyGenerator keyGenerator)
        {
            _logger = logger;
            _dataContext = dataContext;
            _httpServerOptions = httpServerOptions;
            _keyGenerator = keyGenerator;
        }

        [BindProperty]
        public ServerSetup Setup { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile accountKeyFile)
        {
            // Check basic validation first and bail out early before anything gets updated.
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Uri.CheckHostName(Setup.SiteHostname) != UriHostNameType.Dns)
            {
                ModelState.AddModelError("Setup.Domain", "Invalid domain name");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _httpServerOptions.Update(x =>
            {
                x.SiteHostname = Setup.SiteHostname;
                x.HttpsPort = Setup.HttpsPort;
            });

            var acmeAccount = await _dataContext.AcmeAccounts.FirstAsync(x => x.IsAcmeStaging == Setup.UseAcmeStaging);
            var acmeCert = await _dataContext.AcmeCertificates
                        .Include(x => x.AcmeAccount)
                        .ThenInclude(x => x.Key)
                        .FirstOrDefaultAsync(x => x.Subject == Setup.SiteHostname &&
                            x.AcmeAccountId == acmeAccount.AcmeAccountId);

            if (acmeCert == null)
            {
                var certKey = await _dataContext.Keys.FirstOrDefaultAsync(x => x.Name == Setup.SiteHostname);
                if (certKey == null)
                {
                    certKey = _keyGenerator.Generate(Setup.SiteHostname, Certes.KeyAlgorithm.RS256, 
                        "certera certificate (this site)");
                }

                acmeCert = new Data.Models.AcmeCertificate
                {
                    ChallengeType = "http-01",
                    DateCreated = DateTime.UtcNow,
                    Name = Setup.SiteHostname,
                    Subject = Setup.SiteHostname,
                    AcmeAccountId = acmeAccount.AcmeAccountId,
                    KeyId = certKey.KeyId
                };
                _dataContext.AcmeCertificates.Add(acmeCert);
                await _dataContext.SaveChangesAsync();
            }

            return RedirectToPage("./Certificate");
        }
    }

    public class ServerSetup
    {
        [Required]
        [Display(Name = "Site hostname")]
        public string SiteHostname { get; set; }

        [Display(Name = "HTTPS Port")]
        [Range(1, 65535)]
        [DefaultValue(443)]
        public int HttpsPort { get; set; }

        [Display(Name = "ACME Staging")]
        public bool UseAcmeStaging { get; set; }
    }
}