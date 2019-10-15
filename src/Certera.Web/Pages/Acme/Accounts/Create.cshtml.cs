using Certera.Data;
using Certera.Data.Models;
using Certera.Web.AcmeProviders;
using Certera.Web.Services;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Acme.Accounts
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;
        private readonly DataContext _context;
        private readonly KeyGenerator _keyGenerator;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CertesAcmeProvider _certesAcmeProvider;

        public CreateModel(ILogger<CreateModel> logger, DataContext context, KeyGenerator keyGenerator,
            UserManager<ApplicationUser> userManager,
            CertesAcmeProvider certesAcmeProvider)
        {
            _logger = logger;
            _context = context;
            _keyGenerator = keyGenerator;
            _userManager = userManager;
            _certesAcmeProvider = certesAcmeProvider;
        }

        private static string _acmeTos;
        public string TermsOfService
        {
            get
            {
                return _acmeTos;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ApplicationUserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");

            _acmeTos = await GetTermsOfService();

            return Page();
        }

        private async Task<string> GetTermsOfService()
        {
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptV2);
            return (await ctx.TermsOfService()).ToString();
        }

        [BindProperty]
        public Data.Models.AcmeAccount AcmeAccount { get; set; }

        [TempData]
        public string StatusMessage { get; set; }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            bool deleteOnError = false;
            if (AcmeAccount.KeyId < 0)
            {
                var stg = AcmeAccount.IsAcmeStaging ? "-staging" : string.Empty;
                var keyName = $"acme-account{stg}";
                
                var key = _keyGenerator.Generate(keyName, KeyAlgorithm.ES256);
                if (key == null)
                {
                    ModelState.AddModelError(string.Empty, "Error creating key");
                    return Page();
                }

                deleteOnError = true;
                AcmeAccount.Key = key;
            }

            AcmeAccount.ApplicationUser = await _userManager.GetUserAsync(User);

            try
            {
                _context.AcmeAccounts.Add(AcmeAccount);
                await _context.SaveChangesAsync();

                // Create account with key
                await _certesAcmeProvider.CreateAccount(AcmeAccount.AcmeContactEmail,
                    AcmeAccount.Key.RawData, AcmeAccount.IsAcmeStaging);
            }
            catch (Exception e)
            {
                // Delete created key if there's a failure in creating the acme account
                if (deleteOnError)
                {
                    _context.Keys.Remove(AcmeAccount.Key);
                    await _context.SaveChangesAsync();
                }

                _logger.LogError(e, "Error creating ACME account");
                ModelState.AddModelError(string.Empty, "Error creating ACME account");
                return Page();
            }
            
            StatusMessage = "Account created";

            return RedirectToPage("./Index");
        }
    }
}