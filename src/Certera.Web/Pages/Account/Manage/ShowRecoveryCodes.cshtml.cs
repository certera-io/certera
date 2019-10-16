using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Certera.Web.Pages.Account.Manage
{
    public class ShowRecoveryCodesModel : PageModel
    {

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public IList<string> RecoveryCodes { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IActionResult OnGet()
        {
            if (RecoveryCodes == null || RecoveryCodes.Count == 0)
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }

            return Page();
        }
    }
}