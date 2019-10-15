using System;

namespace Certera.Data.Models
{
    public class DomainCertificateChangeEvent
    {
        public long DomainCertificateChangeEventId { get; set; }

        public long NewDomainCertificateId { get; set; }
        public DomainCertificate NewDomainCertificate { get; set; }

        public long PreviousDomainCertificateId { get; set; }
        public DomainCertificate PreviousDomainCertificate { get; set; }

        public long DomainId { get; set; }
        public Domain Domain { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateProcessed { get; set; }
    }
}
