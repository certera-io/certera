using System.ComponentModel.DataAnnotations;

namespace Certera.Data.Models
{
    public class UserConfiguration
    {
        public long UserConfigurationId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Value { get; set; }

        public long ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
