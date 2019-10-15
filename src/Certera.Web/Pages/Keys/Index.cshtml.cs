using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Keys
{
    public class IndexModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;

        public IndexModel(Certera.Data.DataContext context)
        {
            _context = context;
        }

        public IList<Key> Key { get;set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            Key = await _context.Keys.ToListAsync();
        }
    }
}
