using Certes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Certera.Data.Models
{
    public class Key
    {
        public long KeyId { get; set; }

        [Required]
        [RegularExpression("^[a-zA-Z0-9-_.]+$",
            ErrorMessage = "Only alpha-numeric and the following characters: . - _ (no whitespace allowed)")]
        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// PEM encoded.
        /// </summary>
        [Display(Name = "PEM Encoded Key")]
        public string RawData { get; set; }

        [Display(Name = "Created")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Modified")]
        public DateTime DateModified { get; set; }

        [Display(Name = "Rotation")]
        public DateTimeOffset? DateRotationReminder { get; set; }

        [DisplayName("API Key 1")]
        public string ApiKey1 { get; set; }

        [DisplayName("API Key 2")]
        public string ApiKey2 { get; set; }

        public virtual ICollection<KeyHistory> KeyHistories { get; set; } = new List<KeyHistory>();
        public virtual ICollection<AcmeAccount> AcmeAccounts { get; set; } = new List<AcmeAccount>();
        public virtual ICollection<AcmeCertificate> AcmeCertificates { get; set; } = new List<AcmeCertificate>();

        private IKey _ikey;
        public IKey IKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawData))
                {
                    return null;
                }
                if (_ikey == null)
                {
                    _ikey = KeyFactory.FromPem(RawData);
                }
                return _ikey;
            }
        }

        public string Algorithm
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RawData))
                {
                    return null;
                }
                var alg = IKey.Algorithm;
                switch (alg)
                {
                    case KeyAlgorithm.RS256:
                        return "RSA-PKCS1-v1_5 (SHA-256)";
                    case KeyAlgorithm.ES256:
                        return "ECDSA P-256 (SHA-256)";
                    case KeyAlgorithm.ES384:
                        return "ECDSA P-384 (SHA-384)";
                    case KeyAlgorithm.ES512:
                        return "ECDSA P-521 (SHA-512)";
                    default:
                        return "Unknown";
                }
            }
        }
    }
}
