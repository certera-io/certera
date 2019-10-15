using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certera.Data;
using Certera.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Certera.Web.Pages.Tracking
{
    public class HistoryModel : PageModel
    {
        private readonly DataContext _context;

        public HistoryModel(DataContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public Domain Domain { get; set; }

        public IActionResult OnGet(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Domain = _context.Domains
                .Include(x => x.DomainScans)
                .ThenInclude(x => x.DomainCertificate)
                .FirstOrDefault(x => x.DomainId == id.Value);

            if (Domain == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}