using Certera.Core.Helpers;
using Certera.Data;
using Certera.Data.Models;
using Certera.Web.Extensions;
using Certera.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Tracking
{
    public class EditModel : PageModel
    {
        private readonly DataContext _dataContext;
        private readonly DomainScanService _domainScanSvc;

        public EditModel(DataContext dataContext, DomainScanService domainScanSvc)
        {
            _dataContext = dataContext;
            _domainScanSvc = domainScanSvc;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            public string Domains { get; set; }
            public string CertificateFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var allDomains = await _dataContext.Domains
                .OrderBy(x => x.Order)
                .ToListAsync();
            var viewDomains = string.Join(Environment.NewLine, allDomains.Select(x => 
            {
                var domain = x.Uri;
                if (domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    domain = domain.Substring(8);
                }
                return domain;
            }));
            Input = new InputModel { Domains = viewDomains };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile certificateFile)
        {
            if (string.IsNullOrWhiteSpace(Input.Domains) && certificateFile.IsNullOrEmpty())
            {
                ModelState.AddModelError(string.Empty, "Enter some domains or upload a file");
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Input.Domains))
            {
                await UpdateDomains();
            }
            if (!certificateFile.IsNullOrEmpty())
            {
                await AddOrUpdateCertificate(certificateFile);
            }
            return Page();
        }

        private async Task AddOrUpdateCertificate(IFormFile certificateFile)
        {    
            var bytes = await certificateFile.ReadAsBytesAsync();
            X509Certificate2 cert = null;
            try
            {
                cert = new X509Certificate2(bytes);
            }
            catch { }

            if (cert == null)
            {
                ModelState.AddModelError("Input.CertificateFile", "Not a valid certificate");
                return;
            }

            var existingDomainCert = await _dataContext.DomainCertificates
                .FirstOrDefaultAsync(x => x.Thumbprint == cert.Thumbprint);
            if (existingDomainCert != null)
            {
                ModelState.AddModelError("Input.CertificateFile", "Certificate already exists");
            }
            else
            {
                var domainCert = DomainCertificate.FromX509Certificate2(cert, CertificateSource.Uploaded);
                _dataContext.DomainCertificates.Add(domainCert);
                await _dataContext.SaveChangesAsync();

                StatusMessage = "New certificate added";
            }
        }

        private async Task UpdateDomains()
        {
            var currentDomains = _dataContext.Domains
                .ToDictionary(x => x.Uri, x => x);
            var newDomains = new List<Domain>();
            var unchangedDomains = new HashSet<string>();

            // Process domains sent from user, trim and dedupe
            var entries = Input.Domains
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                {
                    var domain = x.Trim();
                    if (!domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        domain = $"https://{domain}";
                    }
                    return domain;
                })
                .Distinct();

            var entriesHashSet = entries.ToHashSet();

            // Use this counter to update the order of the domains the user has specified
            var count = 0;
            foreach (var entry in entries)
            {
                // Ensure entries are valid by checking whether they are malformed and
                // are of the correct protocol
                if (!Uri.IsWellFormedUriString(entry, UriKind.Absolute))
                {
                    ModelState.AddModelError("Input.Domains", $"{entry} - Malformed URI");
                    continue;
                }
                if (!Uri.TryCreate(entry, UriKind.Absolute, out var uri))
                {
                    ModelState.AddModelError("Input.Domains", $"{entry} - Invalid entry");
                    continue;
                }
                if (uri.Scheme != Uri.UriSchemeHttps)
                {
                    ModelState.AddModelError("Input.Domains", $"{entry} - Must be HTTPS scheme");
                    continue;
                }

                // If the domain exists already, update the order and continue
                // on to the next entry.
                if (currentDomains.TryGetValue(entry, out var existingDomain))
                {
                    existingDomain.Order = count++;
                    continue;
                }

                var registrableDomain = DomainParser.RegistrableDomain(uri.Host);
                var domain = new Data.Models.Domain
                {
                    Uri = entry,
                    RegistrableDomain = registrableDomain,
                    Order = count++
                };
                _dataContext.Domains.Add(domain);

                newDomains.Add(domain);
            }

            if (!ModelState.IsValid)
            {
                return;
            }

            // Domains to delete are the ones that are in the current set
            // and not specified by the user
            var domainsToDelete = currentDomains
                .Where(x => !entriesHashSet.Contains(x.Key))
                .Select(x => x.Value)
                .ToList();
            _dataContext.Domains.RemoveRange(domainsToDelete);

            if (ModelState.IsValid && _dataContext.ChangeTracker.HasChanges())
            {
                await _dataContext.SaveChangesAsync();
                StatusMessage = "Domains updated";
            }

            // Trigger scan of newly added domains
            _domainScanSvc.ScanAll(newDomains.Select(x => x.DomainId).ToArray());
        }
    }
}