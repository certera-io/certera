using Certera.Data;
using Certera.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Certera.Web.Services.HostedServices
{
    public class CertificateAcquiryService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IBackgroundTaskQueue _queue;
        private readonly ILogger _logger;
        private Timer _timer;
        private bool _running;

        public CertificateAcquiryService(IServiceProvider services, IBackgroundTaskQueue queue, 
            ILogger<CertificateAcquiryService> logger)
        {
            _services = services;
            _queue = queue;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Certificate discovery service starting.");

            _timer = new Timer(TimerIntervalCallback, null,
                TimeSpan.FromMinutes(5) /* start */,
                TimeSpan.FromMinutes(60) /* interval */);

            return Task.CompletedTask;
        }

        private void TimerIntervalCallback(object state)
        {
            if (_running)
            {
                _logger.LogInformation("Certificate discovery job still running.");
                return;
            }
            _running = true;
            _logger.LogInformation("Certificate discovery job started.");

            RunCertDiscovery();

            _logger.LogInformation("Certificate discovery job completed.");
            _running = false;
        }

        public void RunCertDiscovery()
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var setupOptions = scope.ServiceProvider.GetService<IOptionsSnapshot<Setup>>();
                    if (!setupOptions.Value.Finished)
                    {
                        _logger.LogInformation("Skipping execution of certificate acquiry service because setup is not complete.");
                        return;
                    }

                    var dataContext = scope.ServiceProvider.GetService<DataContext>();

                    // Get all certificates to be considered
                    var allAcmeCerts = dataContext.GetAcmeCertificates();

                    // Find all certificates that have expiration times less than x days.
                    // or that haven't had a request yet.

                    var days = dataContext.GetSetting<int>(Settings.RenewCertificateDays, 30);

                    var allCertsNeedingRenewals = allAcmeCerts.Where(x => x.LatestValidAcmeOrder?.Certificate == null ||
                        (x.LatestValidAcmeOrder?.DomainCertificate != null &&
                         x.LatestValidAcmeOrder.DomainCertificate.ExpiresWithinDays(days)))
                        .ToList();

                    // For each cert, enqueue them to be acquired or renewed
                    var allCertsNeedingAcquiry = allCertsNeedingRenewals
                        .Select(x => x.AcmeCertificateId)
                        .ToList();

                    if (allCertsNeedingAcquiry.Count == 0)
                    {
                        _logger.LogInformation("No certs to acquire");
                        return;
                    }

                    _logger.LogInformation($"{allCertsNeedingAcquiry.Count} certs needing acquiry");
                    var serviceScopeFac = scope.ServiceProvider.GetService<IServiceScopeFactory>();

                    foreach (var id in allCertsNeedingAcquiry)
                    {
                        Enqueue(id, serviceScopeFac);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Certificate discovery job error.");
                }
            }
        }

        private void Enqueue(long id, IServiceScopeFactory serviceScopeFac)
        {
            _queue.QueueBackgroundWorkItem(async token =>
            {
                var localId = id;

                using (var scope = serviceScopeFac.CreateScope())
                {
                    var acquirer = scope.ServiceProvider.GetService<CertificateAcquirer>();
                    await acquirer.AcquireAcmeCert(localId);
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Certificate discovery service stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    _timer?.Dispose();
                }

                disposedValue = true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No unmanaged resources")]
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
