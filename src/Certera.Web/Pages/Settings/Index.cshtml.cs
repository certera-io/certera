using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public ApplicationUser ApplicationUser { get; set; }
        
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ApplicationUser = await _userManager.GetUserAsync(User);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string key)
        {
            var user = await _userManager.GetUserAsync(User);
            switch (key)
            {
                case "apikey1":
                    user.ApiKey1 = ApiKeyGenerator.CreateApiKey();
                    break;
                case "apikey2":
                    user.ApiKey2 = ApiKeyGenerator.CreateApiKey();
                    break;
            }

            await _userManager.UpdateAsync(user);

            ApplicationUser = user;
            return Page();
        }
    }
}