using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Certera.Data.Models
{
    public class AcmeAccount
    {
        public long AcmeAccountId { get; set; }

        [Required]
        [EmailAddress]
        [DisplayName("ACME Contact Email")]
        public string AcmeContactEmail { get; set; }

        [DisplayName("Accept Let's Encrypt Terms of Service")]
        [Range(typeof(bool), "true", "true", ErrorMessage="You must accept the terms of service")]
        public bool AcmeAcceptTos { get; set; }

        [Display(Name = "Created")]
        public DateTime DateCreated { get; set; }
        
        [Display(Name = "Modified")]
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Indicates whether this is associated with the ACME staging site
        /// </summary>
        public bool IsAcmeStaging { get; set; }

        public long KeyId { get; set; }
        public Key Key { get; set; }

        public long ApplicationUserId { get; set; }

        [DisplayName("User")]
        public ApplicationUser ApplicationUser { get; set; }

        public virtual ICollection<AcmeCertificate> AcmeCertificates { get; set; } = new List<AcmeCertificate>();
    }
}
