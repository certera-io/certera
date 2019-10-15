using System;

namespace Certera.Data.Models
{
    public class AcmeRequest
    {
        public long AcmeRequestId { get; set; }
        public DateTime DateCreated { get; set; }

        // The filename, e.g. abcdefg
        // for: /.well-known/acme-challenge/abcdefg
        public string Token { get; set; }

        // The contents of the challenge file
        // e.g. abcdefg.tuvwxyz
        public string KeyAuthorization { get; set; }

        public long AcmeOrderId { get; set; }
        public AcmeOrder AcmeOrder { get; set; }
    }
}
