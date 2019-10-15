using System;

namespace Certera.Data.Models
{
    public class KeyHistory
    {
        public long KeyHistoryId { get; set; }

        public long KeyId { get; set; }
        public Key Key { get; set; }

        public long? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public string Operation { get; set; }
        public DateTime DateOperation { get; set; }
    }
}
