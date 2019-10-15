using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Certera.Data.Models
{
    public class Domain
    {
        public long DomainId { get; set; }
        public string Uri { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastScanned { get; set; }
        public string RegistrableDomain { get; set; }
        public int Order { get; set; }

        public virtual ICollection<DomainScan> DomainScans { get; set; } = new List<DomainScan>();

        [NotMapped]
        public DomainScan LatestDomainScan { get; set; }

        [NotMapped]
        public DomainScan LatestValidDomainScan { get; set; }

        public string HostAndPort()
        {
            try
            {
                return new Uri(Uri).Host;
            }
            catch { }
            return Uri;
        }
    }
}
