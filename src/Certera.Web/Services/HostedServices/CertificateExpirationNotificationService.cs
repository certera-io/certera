using Certera.Core.Mail;
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

                    var mailSenderInfo = scope.ServiceProvider.GetService<IOptionsSnapshot<MailSenderInfo>>();
                    if (string.IsNullOrWhiteSpace(mailSenderInfo?.Value?.Host))
                    {
                        _logger.LogWarning("SMTP not configured. Unable to send certificate expiration notifications.");
                        return;
                    }

                    using (var mailSender = scope.ServiceProvider.GetService<MailSender>())
                    {
                        mailSender.Initialize(mailSenderInfo.Value);

                        var dataContext = scope.ServiceProvider.GetService<DataContext>();

                        var now = DateTime.Now;
                        var in30Days = now.AddDays(30).Date;

                        var certsExpiring = dataContext.GetTrackedCertificates()
                            .Where(x => x.CertId != 0 && x.ValidTo <= in30Days && x.ValidTo >= now)
                            .OrderBy(x => x.ValidTo)
                            .ToList();

                        var expirationBuckets = GroupCertsIntoBuckets(certsExpiring);

                        var certIds = certsExpiring
                            .Select(x => x.CertId)
                            .Distinct()
                            .ToArray();

                        var userNotifications = dataContext.UserNotifications
                            .Where(x => certIds.Contains(x.DomainCertificateId))
                            .ToList()
                            .GroupBy(x => GetNotificationEventKey(x.ApplicationUserId, x.DomainCertificateId, x.NotificationEvent))
                            .ToDictionary(x => x.Key, x => x.First());

                        var userNotificationSettings = dataContext.NotificationSettings
                            .Include(x => x.ApplicationUser)
                            .Where(x =>
                                x.ExpirationAlerts == true &&
                                (x.ExpirationAlert1Day == true ||
                                 x.ExpirationAlert3Days == true ||
                                 x.ExpirationAlert7Days == true ||
                                 x.ExpirationAlert14Days == true ||
                                 x.ExpirationAlert30Days == true));

                        // Consider every single user and build the "view" for each user
                        foreach (var userNotificationSetting in userNotificationSettings)
                        {
                            // What has been sent to that user and what needs to be sent?
                            // If we just came across a certificate and it expires in 3 days, don't also send out
                            // notifications for 7, 14 and 30 days.

                            foreach (var bucket in expirationBuckets)
                            {
                                if (bucket.Value.Any())
                                {
                                    CheckAndSendNotification(userNotificationSetting, bucket, userNotifications, dataContext, mailSender);

                                    // User notified, save the record
                                    dataContext.SaveChanges();
                                }
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
            MailSender mailSender)
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

                        var recipients = new List<string>();
                        recipients.Add(notificationSetting.ApplicationUser.Email);
                        if (!string.IsNullOrWhiteSpace(notificationSetting.AdditionalRecipients))
                        {
                            recipients.AddRange(notificationSetting.AdditionalRecipients.Split(',', ';', StringSplitOptions.RemoveEmptyEntries));
                        }

                        var days = (int)Math.Floor(expiringCert.ValidTo.Value.Subtract(DateTime.Now).TotalDays);
                        var daysText = $"{days} {(days == 1 ? " day" : "days")}";

                        // Show some debug info regarding the reason for the email being sent out
                        _logger.LogDebug($"Send key info: {key}.");

                        var sentNotifications = string.Join(Environment.NewLine, userNotifications.Keys);
                        _logger.LogDebug($"Sent notifications:{Environment.NewLine}{sentNotifications}");

                        _logger.LogInformation($"Sending expiration notification email for {expiringCert.Subject}");

                        mailSender.Send($"[certera] {expiringCert.Subject} - certificate expiration notification",
                            TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateExpiration,
                            new
                            {
                                Domain = expiringCert.Subject,
                                Thumbprint = expiringCert.Thumbprint,
                                DateTime = expiringCert.ValidTo.ToString(),
                                DaysText = daysText,
                                PublicKey = expiringCert.PublicKeyHash,
                                ValidFrom = expiringCert.ValidFrom.Value.ToShortDateString(),
                                ValidTo = expiringCert.ValidTo.Value.ToShortDateString()
                            }),
                            recipients.ToArray());

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

        private static Dictionary<NotificationEvent, List<TrackedCertificate>> GroupCertsIntoBuckets(List<TrackedCertificate> certsExpiring)
        {
            var now = DateTime.Now;
            var in1Day = now.AddDays(1).Date;
            var in3Days = now.AddDays(3).Date;
            var in7Days = now.AddDays(7).Date;
            var in14Days = now.AddDays(14).Date;
            var in30Days = now.AddDays(30).Date;

            return new Dictionary<NotificationEvent, List<TrackedCertificate>>
            {
                { NotificationEvent.ExpirationAlert1Day,
                    certsExpiring.Where(x => x.ValidTo <= in1Day).ToList() },
                { NotificationEvent.ExpirationAlert3Days,
                    certsExpiring.Where(x => x.ValidTo <= in3Days && x.ValidTo > in1Day).ToList() },
                { NotificationEvent.ExpirationAlert7Days,
                    certsExpiring.Where(x => x.ValidTo <= in7Days && x.ValidTo > in3Days).ToList() },
                { NotificationEvent.ExpirationAlert14Days,
                    certsExpiring.Where(x => x.ValidTo <= in14Days && x.ValidTo > in7Days).ToList() },
                { NotificationEvent.ExpirationAlert30Days,
                    certsExpiring.Where(x => x.ValidTo <= in30Days && x.ValidTo > in14Days).ToList() }
            };
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
