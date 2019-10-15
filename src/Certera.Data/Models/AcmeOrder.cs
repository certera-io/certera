using Certes.Acme;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Certera.Data.Models
{
    public class AcmeOrder
    {
        public long AcmeOrderId { get; set; }
        public DateTime DateCreated { get; set; }
        public int RequestCount { get; set; }
        public int InvalidResponseCount { get; set; }
        public AcmeOrderStatus Status { get; set; }
        public string Errors { get; set; }
        public string RawDataPem { get; set; }

        public long AcmeCertificateId { get; set; }
        public AcmeCertificate AcmeCertificate { get; set; }

        public long? DomainCertificateId { get; set; }
        public DomainCertificate DomainCertificate { get; set; }

        public virtual ICollection<AcmeRequest> AcmeRequests { get; set; } = new List<AcmeRequest>();

        public X509Certificate2 Certificate
        {
            get
            {
                if (RawDataPem == null)
                {
                    return null;
                }
                var chain = new CertificateChain(RawDataPem);
                var bytes = chain.Certificate.ToDer();
                var cert = new X509Certificate2(bytes);

                return cert;
            }
        }

        public bool Completed
        {
            get
            {
                return Status == AcmeOrderStatus.Completed;
            }
        }
    }

    public enum AcmeOrderStatus
    {
        Created = 0,
        Challenging,
        Validating,
        Invalid,
        Error,
        Completed
    }
}
