using System.ComponentModel.DataAnnotations;

namespace Certera.Data.Models
{
    public class Setting
    {
        public long SettingId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Value { get; set; }
    }
}
