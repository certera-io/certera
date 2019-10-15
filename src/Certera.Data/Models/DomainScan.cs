using System;

namespace Certera.Data.Models
{
    public class DomainScan
    {
        public long DomainScanId { get; set; }
        public DateTime DateScan { get; set; }
        public bool ScanSuccess { get; set; }
        public string ScanResult { get; set; }
        public string ScanStatus { get; set; }

        public long DomainId { get; set; }
        public Domain Domain { get; set; }

        public long? DomainCertificateId { get; set; }
        public DomainCertificate DomainCertificate { get; set; }

        public long? DomainCertificateChangeEventId { get; set; }
        public DomainCertificateChangeEvent DomainCertificateChangeEvent { get; set; }
    }
}
