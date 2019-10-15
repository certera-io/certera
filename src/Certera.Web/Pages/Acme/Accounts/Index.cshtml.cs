using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Certera.Data;
using Certera.Data.Models;

namespace Certera.Web.Pages.Acme.Accounts
{
    public class IndexModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public IndexModel(Certera.Data.DataContext context)
        {
            _context = context;
        }

        public IList<AcmeAccount> AcmeAccount { get;set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            AcmeAccount = await _context.AcmeAccounts
                .Include(a => a.ApplicationUser)
                .Include(a => a.Key).ToListAsync();
        }
    }
}
