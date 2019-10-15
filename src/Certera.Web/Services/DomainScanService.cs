using Certera.Core.Extensions;
using Certera.Data;
using Certera.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Certera.Web.Services
{
    public class DomainScanService
    {
        private readonly IServiceProvider _services;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public DomainScanService(IServiceProvider services, IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory)
        {
            _services = services;
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        public DomainScan Scan(Domain domain)
        {
            var scanner = new DomainScanner(domain, _services);
            var domainScan = scanner.Scan();

            domain.DateLastScanned = DateTime.UtcNow;

            var domainHasScan = domain.LatestDomainScan?.DomainCertificate != null;
            var thumbprintDifferent = false;

            // Load the last valid scan to ensure we're properly comparing that against the current scan.
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataContext = scope.ServiceProvider.GetService<DataContext>();
                var lastValidScan = dataContext.DomainScans
                    .Include(x => x.DomainCertificate)
                    .Where(x => x.DomainId == domain.DomainId && x.ScanSuccess)
                    .OrderByDescending(x => x.DateScan)
                    .FirstOrDefault();
                thumbprintDifferent = domainHasScan &&
                    domainScan?.DomainCertificate?.Thumbprint != null &&
                    lastValidScan?.DomainCertificate?.Thumbprint != null &&
                    lastValidScan.DomainCertificate.Thumbprint != domainScan.DomainCertificate.Thumbprint;

            }

            if (thumbprintDifferent)
            {
                domainScan.DomainCertificateChangeEvent = new DomainCertificateChangeEvent
                {
                    Domain = domain,
                    DateCreated = DateTime.UtcNow,
                    NewDomainCertificate = domainScan.DomainCertificate,
                    PreviousDomainCertificate = domain.LatestDomainScan.DomainCertificate
                };
            }
            
            // Add the scan result when there hasn't been a successful scan yet (i.e. first time scanning)
            // or when the thumbprint is different (i.e. the cert changed)
            var add = !domainHasScan || thumbprintDifferent;

            if (add)
            {
                domainScan.Domain = domain;
                domain.DomainScans.Add(domainScan);
            }
            return domainScan;
        }

        public void ScanAll(long[] ids = null)
        {
            _queue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var domainScanSvc = scope.ServiceProvider.GetService<DomainScanService>();
                    var dataContext = scope.ServiceProvider.GetService<DataContext>();
                    var domains = dataContext.GetDomains(ids);

                    var batches = domains.Batch(4);
                    foreach (var batch in batches)
                    {
                        batch.AsParallel().ForAll(domain =>
                        {
                            domainScanSvc.Scan(domain);
                        });
                    }
                    await dataContext.SaveChangesAsync();
                }
            });
        }
    }
}
