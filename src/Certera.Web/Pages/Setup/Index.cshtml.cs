using Certera.Data;
using Certera.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Setup
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly DataContext _dataContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private IWritableOptions<Options.Setup> _setupOptions;

        public IndexModel(ILogger<IndexModel> logger,
            DataContext dataContext,
            UserManager<ApplicationUser> userManager,
            IWritableOptions<Options.Setup> setupOptions)
        {
            _logger = logger;
            _dataContext = dataContext;
            _userManager = userManager;
            _setupOptions = setupOptions;
        }

        [BindProperty]
        public AccountSetup Setup { get; set; }

        public bool UserExists { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Setup.Email,
                Email = Setup.Email,
                ApiKey1 = ApiKeyGenerator.CreateApiKey(),
                ApiKey2 = ApiKeyGenerator.CreateApiKey()
            };
            var result = await _userManager.CreateAsync(user, Setup.Password);

            if (result.Succeeded)
            {
                _dataContext.NotificationSettings.Add(new Data.Models.NotificationSetting
                {
                    ApplicationUserId = user.Id
                });
                await _dataContext.SaveChangesAsync();
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("User created a new account with password.");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            return RedirectToPage("./Acme");
        }
    }

    public class AccountSetup
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}