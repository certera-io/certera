using Certera.Core.Extensions;
using Certera.Core.Mail;
using Certera.Data;
using Certera.Data.Models;
using Certera.Web.AcmeProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Certera.Web.Services
{
    public class CertificateAcquirer
    {
        private readonly ILogger<CertificateAcquirer> _logger;
        private readonly DataContext _dataContext;
        private readonly CertesAcmeProvider _certesAcmeProvider;
        private readonly IOptionsSnapshot<MailSenderInfo> _mailSenderInfo;
        private readonly MailSender _mailSender;

        public CertificateAcquirer(ILogger<CertificateAcquirer> logger, DataContext dataContext, CertesAcmeProvider certesAcmeProvider,
            IOptionsSnapshot<MailSenderInfo> mailSenderInfo, MailSender mailSender)
        {
            _logger = logger;
            _dataContext = dataContext;
            _certesAcmeProvider = certesAcmeProvider;
            _mailSenderInfo = mailSenderInfo;
            _mailSender = mailSender;
        }

        public async Task<Data.Models.AcmeOrder> AcquireAcmeCert(long id, bool userRequested = false)
        {
            var acmeCert = await _dataContext.AcmeCertificates
                .Include(x => x.Key)
                .Include(x => x.AcmeAccount)
                .ThenInclude(x => x.Key)
                .SingleAsync(x => x.AcmeCertificateId == id);

            _logger.LogDebug($"[{acmeCert.Subject}] - starting certificate acquisition");
            _certesAcmeProvider.Initialize(acmeCert);

            _logger.LogDebug($"[{acmeCert.Subject}] - creating order");
            var acmeOrder = await _certesAcmeProvider.BeginOrder();
            _dataContext.AcmeOrders.Add(acmeOrder);
            _dataContext.SaveChanges();
            
            _logger.LogDebug($"[{acmeCert.Subject}] - requestion ACME validation");
            await _certesAcmeProvider.Validate();
            _dataContext.SaveChanges();
            
            _logger.LogDebug($"[{acmeCert.Subject}] - completing order");
            await _certesAcmeProvider.Complete();
            acmeOrder.AcmeRequests.Clear();
            _dataContext.SaveChanges();
            
            if (!acmeOrder.Completed)
            {
                _logger.LogError($"[{acmeCert.Subject}] - error obtaining certificate: {acmeOrder.Errors}");
            }

            _logger.LogDebug($"[{acmeCert.Subject}] - done");

            if (acmeOrder.Completed || userRequested)
            {
                return acmeOrder;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(_mailSenderInfo?.Value?.Host))
                {
                    _logger.LogWarning("SMTP not configured. Unable to send certificate change notifications.");
                    return acmeOrder;
                }

                var usersToNotify = _dataContext.NotificationSettings
                        .Include(x => x.ApplicationUser)
                        .Where(x => x.AcquisitionFailureAlerts == true);
                _mailSender.Initialize(_mailSenderInfo.Value);

                foreach (var user in usersToNotify)
                {
                    var recipients = new List<string>();
                    recipients.Add(user.ApplicationUser.Email);
                    if (!string.IsNullOrWhiteSpace(user.AdditionalRecipients))
                    {
                        recipients.AddRange(user.AdditionalRecipients.Split(',', ';', StringSplitOptions.RemoveEmptyEntries));
                    }

                    var lastValidAcmeOrder = _dataContext.AcmeOrders
                        .Include(x => x.DomainCertificate)
                        .Where(x => x.AcmeCertificateId == acmeOrder.AcmeCertificateId)
                        .OrderByDescending(x => x.DateCreated)
                        .FirstOrDefault(x => x.Status == AcmeOrderStatus.Completed);

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
                        sb.AppendLine("<pre>");
                        sb.AppendLine("Current certificate details:");
                        sb.AppendLine($"    Thumbprint:        <b>{thumbprint}</b>");
                        sb.AppendLine($"    Public Key (hash): <b>{publicKey}</b>");
                        sb.AppendLine($"    Valid:             <b>{validFrom} to {validTo}</b>");
                        sb.Append("</pre>");
                        previousCertText = sb.ToString();
                    }

                    _logger.LogInformation($"Sending acquiry failure notification email for {acmeOrder.AcmeCertificate.Name}");

                    _mailSender.Send($"[certera] {acmeOrder.AcmeCertificate.Name} - certificate acquisition failure notification",
                        TemplateManager.BuildTemplate(TemplateManager.NotificationCertificateAcquisitionFailure,
                        new
                        {
                            Domain = acmeOrder.AcmeCertificate.Subject,
                            Error = acmeOrder.Errors,
                            PreviousCertificateDetails = previousCertText,
                            LastAcquiryText = lastAcquiryText
                        }),
                        recipients.ToArray());
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending certificate change notification email");
            }
            return acmeOrder;
        }
    }
}
