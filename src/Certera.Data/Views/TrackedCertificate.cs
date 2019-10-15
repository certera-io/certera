using Certera.Data.Models;
using Certera.Core.Extensions;
using System;

namespace Certera.Data.Views
{
    public class TrackedCertificate
    {
        public long Id { get; set; }
        public long CertId { get; set; }
        public int Order { get; set; }
        public int? DaysRemaining { get; set; }
        public string Subject { get; set; }
        public string RegistrableDomain { get; set; }
        public string Issuer { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool? IsValid { get; set; }
        public string Thumbprint { get; set; }
        public AcmeCertType AcmeCertType { get; set; }
        public CertificateSource Source { get; set; }
        public string PublicKeyHash { get; set; }

        public static TrackedCertificate FromDomain(Domain domain)
        {
            int? daysRemaining = null;
            if (domain.LatestValidDomainScan?.DomainCertificate?.ValidNotAfter != null)
            {
                daysRemaining = (int)Math.Floor(domain.LatestValidDomainScan.DomainCertificate.ValidNotAfter.Subtract(DateTime.Now).TotalDays);
            }

            return new TrackedCertificate
            {
                Id = domain.DomainId,
                CertId = domain?.LatestValidDomainScan?.DomainCertificate?.DomainCertificateId ?? 0,
                Order = domain.Order,
                DaysRemaining = daysRemaining,
                Subject = domain.HostAndPort(),
                RegistrableDomain = domain.RegistrableDomain,
                Issuer = domain?.LatestValidDomainScan?.DomainCertificate?.IssuerName,
                DateModified = domain.DateLastScanned?.ToLocalTime(),
                ValidFrom = domain.LatestValidDomainScan?.DomainCertificate?.ValidNotBefore,
                ValidTo = domain.LatestValidDomainScan?.DomainCertificate?.ValidNotAfter,
                IsValid = domain.LatestValidDomainScan?.DomainCertificate?.IsValidForHostname(domain.Uri),
                Thumbprint = domain.LatestValidDomainScan?.DomainCertificate?.Thumbprint,
                Source = CertificateSource.TrackedDomain,
                PublicKeyHash = domain.LatestValidDomainScan?.DomainCertificate?.Certificate.PublicKeyPinningHash()
            };
        }

        public static TrackedCertificate FromDomainCertificate(DomainCertificate domainCertificate)
        {
            return new TrackedCertificate
            {
                Id = domainCertificate.DomainCertificateId,
                CertId = domainCertificate.DomainCertificateId,
                Order = int.MaxValue,
                DaysRemaining = (int)Math.Floor(domainCertificate.ValidNotAfter.Subtract(DateTime.Now).TotalDays),
                Subject = domainCertificate.Subject,
                RegistrableDomain = domainCertificate.RegistrableDomain,
                Issuer = domainCertificate?.IssuerName,
                DateModified = domainCertificate.DateCreated.ToLocalTime(),
                ValidFrom = domainCertificate.ValidNotBefore,
                ValidTo = domainCertificate.ValidNotAfter,
                IsValid = domainCertificate.Certificate.Verify(),
                Thumbprint = domainCertificate.Thumbprint,
                Source = CertificateSource.Uploaded,
                PublicKeyHash = domainCertificate.Certificate.PublicKeyPinningHash()
            };
        }

        public static TrackedCertificate FromAcmeCertificate(AcmeCertificate cert)
        {
            if (cert.LatestValidAcmeOrder?.DomainCertificate == null)
            {
                return null;
            }
            var trackedCert = FromDomainCertificate(cert.LatestValidAcmeOrder.DomainCertificate);
            trackedCert.Id = cert.AcmeCertificateId;
            trackedCert.Source = CertificateSource.AcmeCertificate;
            trackedCert.AcmeCertType = cert.AcmeAccount.IsAcmeStaging
                ? AcmeCertType.Staging
                : AcmeCertType.Production;

            return trackedCert;
        }
    }

    public enum AcmeCertType
    {
        None,
        Staging,
        Production
    }
}
