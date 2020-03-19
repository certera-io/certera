using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Certera.Data.Models
{
    public class AcmeCertificate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AcmeCertificateId { get; set; }

        [Required]
        [RegularExpression("^[a-zA-Z0-9-_.]+$", 
            ErrorMessage = "Only alpha-numeric and the following characters: . - _ (no whitespace allowed)")]
        public string Name { get; set; }

        [Display(Name = "Created")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Modified")]
        public DateTime DateModified { get; set; }

        [Required]
        public string Subject { get; set; }

        [Display(Name = "Subject Alternative Names")]
        public string SANs { get; set; }

        public long KeyId { get; set; }

        public Key Key { get; set; }

        [Display(Name = "ACME Challenge Type")]
        [RegularExpression("http-01|dns-01")]
        public string ChallengeType { get; set; }

        [Display(Name = "Country (C)")]
        public string CsrCountryName { get; set; }

        [Display(Name = "State (S)")]
        public string CsrState { get; set; }

        [Display(Name = "City (L)")]
        public string CsrLocality { get; set; }

        [Display(Name = "Organization (O)")]
        public string CsrOrganization { get; set; }

        [Display(Name = "Organization Unit (OU)")]
        public string CsrOrganizationUnit { get; set; }

        [Display(Name = "Common Name (CN)")]
        public string CsrCommonName { get; set; }

        public long AcmeAccountId { get; set; }

        public AcmeAccount AcmeAccount { get; set; }

        public virtual ICollection<AcmeOrder> AcmeOrders { get; set; } = new List<AcmeOrder>();

        [DisplayName("API Key 1")]
        public string ApiKey1 { get; set; }

        [DisplayName("API Key 2")]
        public string ApiKey2 { get; set; }

        [NotMapped]
        public AcmeOrder LatestValidAcmeOrder { get; set; }

        public bool IsDnsChallengeType()
        {
            return string.Equals(ChallengeType, "dns-01", StringComparison.OrdinalIgnoreCase);
        }
    }
}
