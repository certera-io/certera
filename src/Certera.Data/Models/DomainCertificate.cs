using Certera.Core.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Certera.Data.Models
{
    public enum CertificateSource
    {
        /// <summary>
        /// A domain that is tracked
        /// </summary>
        TrackedDomain,
        /// <summary>
        /// User uploaded certificate
        /// </summary>
        Uploaded,
        /// <summary>
        /// A certificate obtained via ACME
        /// </summary>
        AcmeCertificate
    }

    public class DomainCertificate
    {
        public long DomainCertificateId { get; set; }

        [DisplayName("Date Created")]
        public DateTime DateCreated { get; set; }

        public string RawData { get; set; }
        public string Thumbprint { get; set; }
        public string SerialNumber { get; set; }
        public DateTime ValidNotBefore { get; set; }
        public DateTime ValidNotAfter { get; set; }
        public string Subject { get; set; }
        public string RegistrableDomain { get; set; }

        [DisplayName("Issuer")]
        public string IssuerName { get; set; }
        public CertificateSource CertificateSource { get; set; }

        public X509Certificate2 Certificate
        {
            get
            {
                if (RawData == null)
                {
                    return null;
                }

                var cert = new X509Certificate2(Convert.FromBase64String(RawData));

                return cert;
            }
        }

        public static DomainCertificate FromX509Certificate2(X509Certificate2 cert, CertificateSource source)
        {
            if (cert == null)
            {
                return null;
            }
            var publicPortion = cert.Export(X509ContentType.Cert);
            var rawData = Convert.ToBase64String(publicPortion);
            var serial = cert.GetSerialNumberString();
            var thumb = cert.GetCertHashString();
            var issuer = cert.GetNameInfo(X509NameType.DnsName, true);
            var subject = cert.GetNameInfo(X509NameType.DnsName, false);
            if (string.IsNullOrWhiteSpace(issuer))
            {
                issuer = cert.Issuer;
            }
            if (string.IsNullOrWhiteSpace(subject))
            {
                if (cert.Subject.StartsWith("CN="))
                {
                    subject = cert.Subject.Substring(3);
                }
                else
                {
                    subject = cert.Subject;
                }                
            }
            var domain = DomainParser.RegistrableDomain(subject);
            var domainCert = new DomainCertificate
            {
                DateCreated = DateTime.UtcNow,
                Subject = subject,
                RegistrableDomain = domain,
                IssuerName = issuer,
                RawData = rawData,
                CertificateSource = source,
                SerialNumber = serial,
                Thumbprint = thumb,
                ValidNotAfter = cert.NotAfter,
                ValidNotBefore = cert.NotBefore
            };
            return domainCert;
        }

        public bool IsValidForHostname(string uri)
        {
            Uri uriObj;
            if (Certificate == null || string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out uriObj))
            {
                return false;
            }

            var sans = ParseSujectAlternativeName(Certificate);

            var matched = false;
            foreach (var san in sans)
            {
                // if host is google.com and wildcard is *.google.com
                // if host is test.google.com and wilcard is *.google.com
                var match = Regex.IsMatch(uriObj.Host, StrippedWildcard(san)) ||
                            Regex.IsMatch(uriObj.Host, WildcardToSubdomain(san));
                if (match)
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }

            if (Certificate.NotAfter <= DateTime.Now || Certificate.NotBefore >= DateTime.Now)
            {
                return false;
            }
            return Certificate.Verify();
        }

        public bool ExpiresWithinDays(int days)
        {
            return DateTime.Now.Date >= ValidNotAfter.Subtract(TimeSpan.FromDays(days)).Date;
        }

        private static string WildcardToSubdomain(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        private static string StrippedWildcard(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*\\.", "") + "$";
        }

        private static List<string> ParseSujectAlternativeName(X509Certificate2 cert)
        {
            var result = new List<string>();

            var subjectAlternativeName = cert.Extensions.Cast<X509Extension>()
                                                .Where(n => n.Oid.FriendlyName.Equals("Subject Alternative Name"))
                                                .Select(n => new AsnEncodedData(n.Oid, n.RawData))
                                                .Select(n => n.Format(true))
                                                .FirstOrDefault();

            if (subjectAlternativeName != null)
            {
                var alternativeNames = subjectAlternativeName.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var alternativeName in alternativeNames)
                {
                    var groups = Regex.Match(alternativeName, @"^DNS Name=(.*)").Groups;

                    if (groups.Count > 0 && !string.IsNullOrEmpty(groups[1].Value))
                    {
                        result.Add(groups[1].Value);
                    }
                }
            }

            return result;
        }
    }
}
