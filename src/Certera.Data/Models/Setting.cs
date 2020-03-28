using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

    public class DnsSettingsContainer
    {
        public string DnsEnvironmentVariables { get; set; }
        public string DnsSetupScript { get; set; }
        public string DnsCleanupScript { get; set; }
        public string DnsSetupScriptArguments { get; set; }
        public string DnsCleanupScriptArguments { get; set; }
    }
}
