using Certera.Core.Extensions;
using Certera.Core.Notifications;
using Certera.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Certera.Web.Services
{
    public class NotificationService : IDisposable
    {
        private readonly MailSender _mailSender;
        private readonly ILogger<NotificationService> _logger;
        private readonly IOptionsSnapshot<MailSenderInfo> _senderInfo;

        public NotificationService(MailSender mailSender, ILogger<NotificationService> logger, IOptionsSnapshot<MailSenderInfo> senderInfo)
        {
            _mailSender = mailSender;
            _logger = logger;
            _senderInfo = senderInfo;
        }

        public void SendDomainCertChangeNotification(IList<NotificationSetting> notificationSettings, IList<DomainCertificateChangeEvent> events)
        {
            bool canSendEmail = InitEmail(notificationSettings);

            foreach (var evt in events)
            {
                foreach (var notification in notificationSettings)
                {
                    if (canSendEmail && notification.SendEmailNotification)
                    {
                        try
                        {
                            _logger.LogInformation($"Sending change notification email for {evt.Domain.HostAndPort()}");

                            var recipients = new List<string>();
                            recipients.Add(notification.ApplicationUser.Email);
                            if (!string.IsNullOrWhiteSpace(notification.AdditionalRecipients))
                            {
                                recipients.AddRange(notification.AdditionalRecipients
                                    .Split(',', ';', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => x.Trim()));
                            }

                            _mailSender.Send($"[certera] {evt.Domain.HostAndPort()} - certificate change notification",
                                TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateChangeEmail,
                                new
                                {
                                    Domain = evt.Domain.HostAndPort(),
                                    NewThumbprint = evt.NewDomainCertificate.Thumbprint,
                                    NewPublicKey = evt.NewDomainCertificate.Certificate.PublicKeyPinningHash(),
                                    NewValidFrom = evt.NewDomainCertificate.ValidNotBefore.ToShortDateString(),
                                    NewValidTo = evt.NewDomainCertificate.ValidNotAfter.ToShortDateString(),
                                    PreviousThumbprint = evt.PreviousDomainCertificate.Thumbprint,
                                    PreviousPublicKey = evt.PreviousDomainCertificate.Certificate.PublicKeyPinningHash(),
                                    PreviousValidFrom = evt.PreviousDomainCertificate.ValidNotBefore.ToShortDateString(),
                                    PreviousValidTo = evt.PreviousDomainCertificate.ValidNotAfter.ToShortDateString()
                                }),
                                recipients.ToArray());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending certificate change notification email");
                        }
                    }

                    if (notification.SendSlackNotification)
                    {
                        try
                        {
                            _logger.LogInformation($"Sending change notification slack for {evt.Domain.HostAndPort()}");

                            var json = TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateChangeSlack,
                                new
                                {
                                    Domain = evt.Domain.HostAndPort(),
                                    NewThumbprint = evt.NewDomainCertificate.Thumbprint,
                                    NewPublicKey = evt.NewDomainCertificate.Certificate.PublicKeyPinningHash(),
                                    NewValidFrom = evt.NewDomainCertificate.ValidNotBefore.ToShortDateString(),
                                    NewValidTo = evt.NewDomainCertificate.ValidNotAfter.ToShortDateString(),
                                    PreviousThumbprint = evt.PreviousDomainCertificate.Thumbprint,
                                    PreviousPublicKey = evt.PreviousDomainCertificate.Certificate.PublicKeyPinningHash(),
                                    PreviousValidFrom = evt.PreviousDomainCertificate.ValidNotBefore.ToShortDateString(),
                                    PreviousValidTo = evt.PreviousDomainCertificate.ValidNotAfter.ToShortDateString()
                                });

                            SendSlack(notification.SlackWebhookUrl, json);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending certificate change notification email");
                        }
                    }
                }

                // Mark the event as processed so it doesn't show up in the next query
                evt.DateProcessed = DateTime.UtcNow;
            }
        }

        public void SendCertAcquitionFailureNotification(IList<NotificationSetting> notificationSettings,
            AcmeOrder acmeOrder, AcmeOrder lastValidAcmeOrder)
        {
            bool canSendEmail = InitEmail(notificationSettings);

            foreach (var notification in notificationSettings)
            {
                if (canSendEmail && notification.SendEmailNotification)
                {
                    try
                    {
                        var recipients = new List<string>();
                        recipients.Add(notification.ApplicationUser.Email);
                        if (!string.IsNullOrWhiteSpace(notification.AdditionalRecipients))
                        {
                            recipients.AddRange(notification.AdditionalRecipients
                                .Split(',', ';', StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim()));
                        }

                        string previousCertText = string.Empty;
                        string lastAcquiryText = "Never";

                        if (lastValidAcmeOrder?.DomainCertificate != null)
                        {
                            lastAcquiryText = lastValidAcmeOrder.DateCreated.ToString();

                            var thumbprint = lastValidAcmeOrder.DomainCertificate.Thumbprint;
                            var publicKey = lastValidAcmeOrder.DomainCertificate.Certificate.PublicKeyPinningHash();
                            var validFrom = lastValidAcmeOrder.DomainCertificate.ValidNotBefore.ToShortDateString();
                            var validTo = lastValidAcmeOrder.DomainCertificate.ValidNotAfter.ToShortDateString();

                            var sb = new StringBuilder();
                            sb.AppendLine("<u>Current certificate details</u>");
                            sb.AppendLine();
                            sb.AppendLine("<b>Thumbprint</b>");
                            sb.AppendLine($"{thumbprint}");
                            sb.AppendLine();
                            sb.AppendLine("<b>Public Key (hash)</b>");
                            sb.AppendLine($"{publicKey}");
                            sb.AppendLine();
                            sb.AppendLine("<b>Valid</b>");
                            sb.AppendLine($"{validFrom} to {validTo}");
                            previousCertText = sb.ToString();
                        }

                        _logger.LogInformation($"Sending certificate acquisition failure notification email for {acmeOrder.AcmeCertificate.Name}");

                        _mailSender.Send($"[certera] {acmeOrder.AcmeCertificate.Name} - certificate acquisition failure notification",
                            TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateAcquisitionFailureEmail,
                            new
                            {
                                Domain = acmeOrder.AcmeCertificate.Subject,
                                Error = acmeOrder.Errors,
                                PreviousCertificateDetails = previousCertText,
                                LastAcquiryText = lastAcquiryText
                            }),
                            recipients.ToArray());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending certificate acquisition failure notification email");
                    }
                }

                if (notification.SendSlackNotification)
                {
                    try
                    {
                        _logger.LogInformation($"Sending acquisition failure notification slack for {acmeOrder.AcmeCertificate.Name}");

                        string previousCertText = string.Empty;
                        string lastAcquiryText = "Never";

                        if (lastValidAcmeOrder?.DomainCertificate != null)
                        {
                            lastAcquiryText = lastValidAcmeOrder.DateCreated.ToString();

                            var thumbprint = lastValidAcmeOrder.DomainCertificate.Thumbprint;
                            var publicKey = lastValidAcmeOrder.DomainCertificate.Certificate.PublicKeyPinningHash();
                            var validFrom = lastValidAcmeOrder.DomainCertificate.ValidNotBefore.ToShortDateString();
                            var validTo = lastValidAcmeOrder.DomainCertificate.ValidNotAfter.ToShortDateString();

                            var sb = new StringBuilder();
                            sb.Append("*Current certificate details*\n");
                            sb.Append($"*Thumbprint:*\n{thumbprint}\n");
                            sb.Append($"*Public Key (hash):*\n{publicKey}\n");
                            sb.Append($"*Valid:*\n{validFrom} to {validTo}");
                            previousCertText = sb.ToString();
                        }

                        var json = TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateAcquisitionFailureSlack,
                            new
                            {
                                Domain = acmeOrder.AcmeCertificate.Subject,
                                Error = acmeOrder.Errors,
                                PreviousCertificateDetails = previousCertText,
                                LastAcquiryText = lastAcquiryText
                            });

                        SendSlack(notification.SlackWebhookUrl, json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending certificate acquisition failure notification slack");
                    }
                }
            }
        }

        public void SendExpirationNotification(NotificationSetting notificationSetting, Data.Views.TrackedCertificate expiringCert)
        {
            var days = (int)Math.Floor(expiringCert.ValidTo.Value.Subtract(DateTime.Now).TotalDays);
            var daysText = $"{days} {(days == 1 ? " day" : "days")}";

            bool canSendEmail = InitEmail(new List<NotificationSetting> { notificationSetting });
            if (canSendEmail && notificationSetting.SendEmailNotification)
            {
                try
                {
                    _logger.LogInformation($"Sending certificate expiration notification email for {expiringCert.Subject}");

                    var recipients = new List<string>();
                    recipients.Add(notificationSetting.ApplicationUser.Email);
                    if (!string.IsNullOrWhiteSpace(notificationSetting.AdditionalRecipients))
                    {
                        recipients.AddRange(notificationSetting.AdditionalRecipients
                            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()));
                    }

                    _mailSender.Send($"[certera] {expiringCert.Subject} - certificate expiration notification",
                        TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateExpirationEmail,
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending certificate expiration notification email");
                }
            }

            if (notificationSetting.SendSlackNotification)
            {
                try
                {
                    _logger.LogInformation($"Sending certificate expiration notification slack for {expiringCert.Subject}");

                    var json = TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateExpirationSlack,
                        new
                        {
                            Domain = expiringCert.Subject,
                            Thumbprint = expiringCert.Thumbprint,
                            DateTime = expiringCert.ValidTo.ToString(),
                            DaysText = daysText,
                            PublicKey = expiringCert.PublicKeyHash,
                            ValidFrom = expiringCert.ValidFrom.Value.ToShortDateString(),
                            ValidTo = expiringCert.ValidTo.Value.ToShortDateString()
                        });

                    SendSlack(notificationSetting.SlackWebhookUrl, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending certificate expiration notification slack");
                }
            }
        }

        private void SendSlack(string slackUrl, string json)
        {
            using (var client = new WebClient())
            {
                var data = new NameValueCollection();
                data["payload"] = json;

                var tries = 3;
                while (tries > 0)
                {
                    try
                    {
                        var response = client.UploadValues(slackUrl, "POST", data);

                        string responseText = Encoding.UTF8.GetString(response);
                        _logger.LogDebug($"Slack response: {responseText}");

                        break;
                    }
                    catch (WebException we)
                    {
                        string errorResponse = null;
                        if (we.Response != null)
                        {
                            using (WebResponse response = we.Response)
                            {
                                var stream = response.GetResponseStream();
                                using (var reader = new StreamReader(stream))
                                {
                                    errorResponse = reader.ReadToEnd();
                                }
                            }
                        }
                        _logger.LogError($"Error sending to slack. {we.Status}. {we.Message}. {errorResponse}");
                    }
                    tries--;
                }
            }
        }

        private bool InitEmail(IList<NotificationSetting> notificationSettings)
        {
            var canSendEmail = !string.IsNullOrWhiteSpace(_senderInfo?.Value?.Host);
            var sendingEmail = notificationSettings.Any(x => x.SendEmailNotification);

            if (sendingEmail && !canSendEmail)
            {
                _logger.LogWarning("SMTP not configured. Unable to send certificate change notifications via email.");
            }
            else if (sendingEmail && canSendEmail)
            {
                _mailSender.Initialize(_senderInfo.Value);
            }

            return canSendEmail;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _mailSender?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
