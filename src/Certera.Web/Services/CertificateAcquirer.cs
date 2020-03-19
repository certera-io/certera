using Certera.Data;
using Certera.Data.Models;
using Certera.Web.AcmeProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Services
{
    public class CertificateAcquirer
    {
        private readonly ILogger<CertificateAcquirer> _logger;
        private readonly DataContext _dataContext;
        private readonly CertesAcmeProvider _certesAcmeProvider;
        private readonly NotificationService _notificationService;

        public CertificateAcquirer(ILogger<CertificateAcquirer> logger, DataContext dataContext, CertesAcmeProvider certesAcmeProvider,
            NotificationService notificationService)
        {
            _logger = logger;
            _dataContext = dataContext;
            _certesAcmeProvider = certesAcmeProvider;
            _notificationService = notificationService;
        }

        public async Task<AcmeOrder> AcquireAcmeCert(long id, bool userRequested = false)
        {
            var acmeCert = await _dataContext.AcmeCertificates
                .Include(x => x.Key)
                .Include(x => x.AcmeAccount)
                .ThenInclude(x => x.Key)
                .SingleAsync(x => x.AcmeCertificateId == id);

            var dnsSettings = _dataContext.GetDnsSettings();

            _logger.LogDebug($"[{acmeCert.Subject}] - starting certificate acquisition");
            _certesAcmeProvider.Initialize(acmeCert);

            _logger.LogDebug($"[{acmeCert.Subject}] - creating ACME order");
            var acmeOrder = await _certesAcmeProvider.BeginOrder();
            _dataContext.AcmeOrders.Add(acmeOrder);
            _dataContext.SaveChanges();

            try
            {
                if (acmeCert.IsDnsChallengeType())
                {
                    _logger.LogDebug($"[{acmeCert.Subject}] - setting DNS records");
                    var dnsSetResult = _certesAcmeProvider.SetDnsRecords(dnsSettings);

                    if (dnsSetResult)
                    {
                        _logger.LogDebug($"[{acmeCert.Subject}] - validating DNS records");
                        var dnsValidateResult = await _certesAcmeProvider.ValidateDnsRecords();
                    }
                }

                _logger.LogDebug($"[{acmeCert.Subject}] - requesting ACME validation");
                await _certesAcmeProvider.Validate();
                _dataContext.SaveChanges();

                _logger.LogDebug($"[{acmeCert.Subject}] - completing order");
                await _certesAcmeProvider.Complete();

                if (acmeCert.IsDnsChallengeType())
                {
                    _logger.LogDebug($"[{acmeCert.Subject}] - cleaning up DNS records");
                    _certesAcmeProvider.CleanupDnsRecords(dnsSettings);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                acmeOrder.AcmeRequests.Clear();
                _dataContext.SaveChanges();
            }

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
                var notificationSettings = _dataContext.NotificationSettings
                        .Include(x => x.ApplicationUser)
                        .Where(x => x.AcquisitionFailureAlerts == true)
                        .ToList();

                var lastValidAcmeOrder = _dataContext.AcmeOrders
                    .Include(x => x.DomainCertificate)
                    .Where(x => x.AcmeCertificateId == acmeOrder.AcmeCertificateId)
                    .OrderByDescending(x => x.DateCreated)
                    .FirstOrDefault(x => x.Status == AcmeOrderStatus.Completed);

                _notificationService.SendCertAcquitionFailureNotification(notificationSettings, acmeOrder, lastValidAcmeOrder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending certificate acquisition failure notification");
            }

            return acmeOrder;
        }
    }
}
