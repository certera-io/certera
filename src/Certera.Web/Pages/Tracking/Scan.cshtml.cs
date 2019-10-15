using Certera.Data;
using Certera.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Tracking
{
    public class ScanModel : PageModel
    {
        private readonly DataContext _dataContext;
        private readonly DomainScanService _domainScanSvc;

        public ScanModel(DataContext dataContext, DomainScanService domainScanSvc)
        {
            _dataContext = dataContext;
            _domainScanSvc = domainScanSvc;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGet(long? id = null, string returnUrl = null)
        {
            if (id != null)
            {
                var domain = _dataContext.GetDomain(id.Value);

                if (domain == null)
                {
                    StatusMessage = "Domain not found";
                    return RedirectToPage("./Index");
                }

                var scan = _domainScanSvc.Scan(domain);
                await _dataContext.SaveChangesAsync();

                StatusMessage = scan.ScanSuccess ? "Domain scanned successfully" : "Domain scan failed";
            }
            else
            {
                // Schedule a run for all domains to be scanned
                _domainScanSvc.ScanAll();
                StatusMessage = "Domain scan queued";
            }
            returnUrl = returnUrl ?? Url.Page("./Index");
            return new RedirectResult(returnUrl);
        }
    }
}