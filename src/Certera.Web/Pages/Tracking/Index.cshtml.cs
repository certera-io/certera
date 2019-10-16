using Certera.Data;
using Certera.Data.Views;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Tracking
{
    public class IndexModel : PageModel
    {
        private readonly DataContext _dataContext;

        public IndexModel(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public List<TrackedCertificate> TrackedCertificates { get; set; }
        public List<SelectListItem> Sort { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult OnGet(string sort)
        {
            if (!string.IsNullOrWhiteSpace(sort))
            {
                sort = sort.ToLower();
            }

            Expression<Func<TrackedCertificate, object>> sortExpression = null;
            Expression<Func<TrackedCertificate, object>> sortThenByExpression = x => x.Subject;

            switch (sort)
            {
                case "expiration":
                    sortExpression = x => x.DaysRemaining;
                    break;
                case "subject":
                    sortExpression = x => x.Subject;
                    break;
                case "issuer":
                    sortExpression = x => x.Issuer;
                    break;
                case "domain":
                    sortExpression = x => x.RegistrableDomain;
                    break;
                case "source":
                    sortExpression = x => x.Source;
                    break;
                case "order":
                default:
                    sortExpression = x => x.Order;
                    sortThenByExpression = x => x.DateModified;
                    break;
            }

            Sort = new List<SelectListItem>
            {
                new SelectListItem { Text = "Order", Value = "order", Selected = sort == "order" },
                new SelectListItem { Text = "Expiration", Value = "expiration", Selected = sort == "expiration" },
                new SelectListItem { Text = "Subject", Value = "subject", Selected = sort == "subject" },
                new SelectListItem { Text = "Domain", Value = "domain", Selected = sort == "domain" },
                new SelectListItem { Text = "Issuer", Value = "issuer", Selected = sort == "issuer" },
                new SelectListItem { Text = "Source", Value = "source", Selected = sort == "source" }
            };

            var allTrackedCerts = _dataContext.GetTrackedCertificates();

            TrackedCertificates = allTrackedCerts
                .AsQueryable()
                .OrderBy(sortExpression)
                .ThenBy(sortThenByExpression)
                .ToList();

            return Page();
        }
    }
}
