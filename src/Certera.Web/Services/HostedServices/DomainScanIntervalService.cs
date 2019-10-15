using Certera.Core.Extensions;
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
    public class DomainScanIntervalService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private Timer _timer;
        private bool _running;

        public DomainScanIntervalService(IServiceProvider services, ILogger<DomainScanIntervalService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Domain scanning service starting.");

            _timer = new Timer(TimerIntervalCallback, null,
                TimeSpan.FromMinutes(1) /* start */,
                TimeSpan.FromMinutes(60) /* interval */);

            return Task.CompletedTask;
        }

        private void TimerIntervalCallback(object state)
        {
            if (_running)
            {
                _logger.LogInformation("Domain scanning job still running.");
                return;
            }
            _running = true;
            _logger.LogInformation("Domain scanning job started.");

            RunDomainScan();

            _logger.LogInformation("Domain scanning job completed.");
            _running = false;
        }

        private void RunDomainScan()
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var setupOptions = scope.ServiceProvider.GetService<IOptionsSnapshot<Setup>>();
                    if (!setupOptions.Value.Finished)
                    {
                        _logger.LogInformation("Skipping execution of domain scan service because setup is not complete.");
                        return;
                    }

                    var domainScanSvc = scope.ServiceProvider.GetService<DomainScanService>();
                    var dataContext = scope.ServiceProvider.GetService<DataContext>();

                    var fourHoursAgo = DateTime.UtcNow.AddHours(-4);
                    var domainsNeedingScan = dataContext.GetDomainsNeedingScan(fourHoursAgo);

                    if (domainsNeedingScan.Count == 0)
                    {
                        _logger.LogInformation("No domains needing scan");
                        return;
                    }

                    _logger.LogInformation($"{domainsNeedingScan.Count} domains to scan");

                    var batches = domainsNeedingScan.Batch(4);
                    foreach (var batch in batches)
                    {
                        batch.AsParallel().ForAll(domain =>
                        {
                            domainScanSvc.Scan(domain);
                        });
                    }
                    dataContext.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Domain scanning job error.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Domain scanning service stopping.");

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
