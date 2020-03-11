using Certera.Data;
using Certera.Data.Models;
using Certera.Data.Views;
using Certera.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Certera.Web.Services.HostedServices
{
    public class CertificateExpirationNotificationService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private Timer _timer;
        private bool _running;

        public CertificateExpirationNotificationService(IServiceProvider services, 
            ILogger<CertificateExpirationNotificationService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Certificate expiration notification service starting.");

            _timer = new Timer(TimerIntervalCallback, null,
                TimeSpan.FromMinutes(2) /* start */,
                TimeSpan.FromMinutes(60) /* interval */);

            return Task.CompletedTask;
        }

        private void TimerIntervalCallback(object state)
        {
            if (_running)
            {
                _logger.LogInformation("Certificate expiration notification job still running.");
                return;
            }
            _running = true;
            _logger.LogInformation("Certificate expiration notification job started.");

            RunNotificationCheck();

            _logger.LogInformation("Certificate expiration notification job completed.");
            _running = false;
        }

        private void RunNotificationCheck()
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var setupOptions = scope.ServiceProvider.GetService<IOptionsSnapshot<Setup>>();
                    if (!setupOptions.Value.Finished)
                    {
                        _logger.LogInformation("Skipping execution of certificate expiration notification service because setup is not complete.");
                        return;
                    }

                    var dataContext = scope.ServiceProvider.GetService<DataContext>();
                    var notificationService = scope.ServiceProvider.GetService<NotificationService>();

                    var now = DateTime.Now;
                    var in30Days = now.AddDays(31).Date;

                    var certsExpiring = dataContext.GetTrackedCertificates()
                        .Where(x => x.CertId != 0 && x.ValidTo <= in30Days && x.ValidTo >= now)
                        .OrderBy(x => x.ValidTo)
                        .ToList();

                    var expirationBuckets = GroupCertsIntoBuckets(now, certsExpiring);

                    var certIds = certsExpiring
                        .Select(x => x.CertId)
                        .Distinct()
                        .ToArray();

                    var userNotifications = dataContext.UserNotifications
                        .Where(x => certIds.Contains(x.DomainCertificateId))
                        .ToList()
                        .GroupBy(x => GetNotificationEventKey(x.ApplicationUserId, x.DomainCertificateId, x.NotificationEvent))
                        .ToDictionary(x => x.Key, x => x.First());

                    var notificationSettings = dataContext.NotificationSettings
                        .Include(x => x.ApplicationUser)
                        .Where(x =>
                            x.ExpirationAlerts == true &&
                            (x.ExpirationAlert1Day == true ||
                             x.ExpirationAlert3Days == true ||
                             x.ExpirationAlert7Days == true ||
                             x.ExpirationAlert14Days == true ||
                             x.ExpirationAlert30Days == true));

                    // Consider every single user and build the "view" for each user
                    foreach (var notificationSetting in notificationSettings)
                    {
                        // What has been sent to that user and what needs to be sent?
                        // If we just came across a certificate and it expires in 3 days, don't also send out
                        // notifications for 7, 14 and 30 days.

                        foreach (var bucket in expirationBuckets)
                        {
                            if (bucket.Value.Any())
                            {
                                CheckAndSendNotification(notificationSetting, bucket, userNotifications, dataContext, notificationService);

                                // User notified, save the record
                                dataContext.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Certificate expiration notification job error.");
                }
            }
        }

        private void CheckAndSendNotification(NotificationSetting notificationSetting,
            KeyValuePair<NotificationEvent, List<TrackedCertificate>> bucket,
            Dictionary<string, UserNotification> userNotifications,
            DataContext dataContext,
            NotificationService notificationService)
        {
            foreach(var expiringCert in bucket.Value)
            {
                try
                {
                    var key = GetNotificationEventKey(notificationSetting.ApplicationUserId, expiringCert.CertId, bucket.Key);
                    if (!userNotifications.ContainsKey(key))
                    {
                        if (!ShouldSend(notificationSetting, bucket.Key))
                        {
                            continue;
                        }

                        // Show some debug info regarding the reason for the email being sent out
                        _logger.LogDebug($"Send key info: {key}.");

                        var sentNotifications = string.Join(Environment.NewLine, userNotifications.Keys);
                        _logger.LogDebug($"Sent notifications:{Environment.NewLine}{sentNotifications}");

                        _logger.LogInformation($"Sending expiration notification email for {expiringCert.Subject}");

                        notificationService.SendExpirationNotification(notificationSetting, expiringCert);

                        // Save the notification so the user isn't notified again for this certificate and this time period
                        var userNotification = new UserNotification
                        {
                            ApplicationUser = notificationSetting.ApplicationUser,
                            DateCreated = DateTime.UtcNow,
                            DomainCertificateId = expiringCert.CertId,
                            NotificationEvent = bucket.Key
                        };
                        dataContext.UserNotifications.Add(userNotification);

                        userNotifications.Add(key, userNotification);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error sending certificate expiration email");
                }
            }
        }

        public static Dictionary<NotificationEvent, List<TrackedCertificate>> GroupCertsIntoBuckets(DateTime now, List<TrackedCertificate> certsExpiring)
        {
            // add an extra day because we're using .Date, which defaults to 12:00 AM of the given day.
            var in1Day = now.AddDays(2).Date;
            var in3Days = now.AddDays(4).Date;
            var in7Days = now.AddDays(8).Date;
            var in14Days = now.AddDays(15).Date;
            var in30Days = now.AddDays(31).Date;

            var dict = new Dictionary<NotificationEvent, List<TrackedCertificate>>
            {
                { NotificationEvent.ExpirationAlert1Day, new List<TrackedCertificate>() },
                { NotificationEvent.ExpirationAlert3Days, new List<TrackedCertificate>() },
                { NotificationEvent.ExpirationAlert7Days, new List<TrackedCertificate>() },
                { NotificationEvent.ExpirationAlert14Days, new List<TrackedCertificate>() },
                { NotificationEvent.ExpirationAlert30Days, new List<TrackedCertificate>() }
            };

            foreach (var cert in certsExpiring)
            {
                if (cert.ValidTo <= in1Day && cert.ValidTo > now)
                {
                    dict[NotificationEvent.ExpirationAlert1Day].Add(cert);
                }
                else if (cert.ValidTo <= in3Days && cert.ValidTo > in1Day)
                {
                    dict[NotificationEvent.ExpirationAlert3Days].Add(cert);
                }
                else if (cert.ValidTo <= in7Days && cert.ValidTo > in3Days)
                {
                    dict[NotificationEvent.ExpirationAlert7Days].Add(cert);
                }
                else if (cert.ValidTo <= in14Days && cert.ValidTo > in7Days)
                {
                    dict[NotificationEvent.ExpirationAlert14Days].Add(cert);
                }
                else if (cert.ValidTo <= in30Days && cert.ValidTo > in14Days)
                {
                    dict[NotificationEvent.ExpirationAlert30Days].Add(cert);
                }
            }

            return dict;
        }

        private static string GetNotificationEventKey(long userId, long certId, NotificationEvent notificationEvent)
        {
            return $"{userId}:{certId}:{notificationEvent}";
        }

        private static bool ShouldSend(NotificationSetting notificationSetting, NotificationEvent notificationEvent)
        {
            return (bool)notificationSetting.GetType()
                            .GetProperty(notificationEvent.ToString())
                            .GetValue(notificationSetting);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Certificate expiration notification service stopping.");

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
