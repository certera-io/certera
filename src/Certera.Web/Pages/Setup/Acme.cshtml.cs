using Certera.Data;
using Certera.Data.Models;
using Certera.Web.AcmeProviders;
using Certera.Web.Extensions;
using Certera.Web.Options;
using Certera.Web.Services;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Setup
{
    public class AcmeModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly DataContext _dataContext;
        private readonly CertesAcmeProvider _certesAcmeProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWritableOptions<HttpServer> _httpServerOptions;
        private readonly IWritableOptions<Options.Setup> _setupOptions;
        private readonly KeyGenerator _keyGenerator;

        public AcmeModel(ILogger<IndexModel> logger,
            DataContext dataContext,
            CertesAcmeProvider certesAcmeProvider,
            UserManager<ApplicationUser> userManager,
            IWritableOptions<HttpServer> httpServerOptions,
            IWritableOptions<Options.Setup> setupOptions,
            KeyGenerator keyGenerator)
        {
            _logger = logger;
            _dataContext = dataContext;
            _certesAcmeProvider = certesAcmeProvider;
            _userManager = userManager;
            _httpServerOptions = httpServerOptions;
            _setupOptions = setupOptions;
            _keyGenerator = keyGenerator;
        }

        [BindProperty]
        public AcmeSetup Setup { get; set; }

        private static string _acmeTos;

        public string TermsOfService
        {
            get
            {
                return _acmeTos;
            }
        }

        public bool UserExists { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = _dataContext.ApplicationUsers.First();
            Setup = new AcmeSetup
            {
                AcmeContactEmail = user.Email
            };

            _acmeTos = await GetTermsOfService();

            return Page();
        }

        private async Task<string> GetTermsOfService()
        {
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptV2);
            return (await ctx.TermsOfService()).ToString();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile accountKeyFile)
        {
            // Check basic validation first and bail out early before anything gets updated.
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = _dataContext.ApplicationUsers.First();
            var keyContents = await accountKeyFile.ReadAsStringAsync();
            var key = await CreateKeyIfNotExists(user, keyContents, false);
            var acmeAccount = await CreateOrUpdateAcmeAccount(user, key, false);
            await EnsureLetsEncryptAccountExists(acmeAccount, false);

            var stagingKey = await CreateKeyIfNotExists(user, null, true);
            var stagingAcmeAccount = await CreateOrUpdateAcmeAccount(user, stagingKey, true);
            await EnsureLetsEncryptAccountExists(stagingAcmeAccount, true);

            return RedirectToPage("./Server");
        }

        private async Task<Data.Models.AcmeAccount> CreateOrUpdateAcmeAccount(ApplicationUser user, Key key, bool staging)
        {
            // Setup.AcmeContactEmail explicitly specified.
            // Check for existing account, create new if not exists, use key if one specified or
            // create and store new key.
            Data.Models.AcmeAccount acmeAccount = null;

            if (!string.IsNullOrWhiteSpace(Setup.AcmeContactEmail))
            {
                acmeAccount = await _dataContext.AcmeAccounts
                    .FirstOrDefaultAsync(x => x.AcmeContactEmail == Setup.AcmeContactEmail && 
                        x.IsAcmeStaging == staging);
            }

            // Try to locate using user's email
            if (acmeAccount == null)
            {
                acmeAccount = await _dataContext.AcmeAccounts
                    .FirstOrDefaultAsync(x => x.AcmeContactEmail == user.Email &&
                        x.IsAcmeStaging == staging);
            }

            // No account exists for user, create new ACME account
            if (acmeAccount == null)
            {
                var emailToUse = Setup.AcmeContactEmail ?? user.Email;
                acmeAccount = new Data.Models.AcmeAccount
                {
                    AcmeAcceptTos = true,
                    AcmeContactEmail = emailToUse,
                    Key = key,
                    ApplicationUser = user,
                    IsAcmeStaging = staging
                };
                _dataContext.AcmeAccounts.Add(acmeAccount);
                await _dataContext.SaveChangesAsync();
            }
            return acmeAccount;
        }

        private async Task EnsureLetsEncryptAccountExists(Data.Models.AcmeAccount acmeAccount, bool staging)
        {
            var accountExists = await _certesAcmeProvider.AccountExists(acmeAccount.Key.RawData, staging);

            if (accountExists)
            {
                _logger.LogDebug("ACME account already exists.");
            }
            else
            {
                _logger.LogDebug("ACME account does not exists, creating account using existing key.");

                // Create account with existing key
                await _certesAcmeProvider.CreateAccount(acmeAccount.AcmeContactEmail, 
                    acmeAccount.Key.RawData, staging);
            }
        }

        private async Task<Key> CreateKeyIfNotExists(ApplicationUser user, string keyContents, bool staging)
        {
            var stg = staging ? "-staging" : string.Empty;
            var keyName = $"user-{user.Id}-acme-account{stg}";
            var key = await _dataContext.Keys.FirstOrDefaultAsync(x => x.Name == keyName);
            
            if (key == null)
            {
                var desc = $"Let's Encrypt {(staging ? "Staging " : string.Empty)}Account Key";
                key = _keyGenerator.Generate(keyName, KeyAlgorithm.ES256, desc, keyContents);
            }

            return key;
        }
    }

    public class AcmeSetup
    {
        [Display(Name = "Let's Encrypt Account Key")]
        public string AccountKeyFile { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "ACME Contact Email")]
        public string AcmeContactEmail { get; set; }

        [Display(Name = "Accept Let's Encrypt Terms of Service")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms of service")]
        public bool AcmeAcceptTos { get; set; }
    }
}