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

        private List<KeyValuePair<string, string>> _cachedStringDict;

        public List<KeyValuePair<string, string>> TransformEnvironmentVariables()
        {
            if (_cachedStringDict != null)
            {
                return _cachedStringDict;
            }

            _cachedStringDict = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(DnsEnvironmentVariables))
            {
                var lines = DnsEnvironmentVariables.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split("=", 2);
                    if (parts.Length > 0) 
                    {
                        var envKey = parts[0];
                        string value = null;
                        if (parts.Length >= 1)
                        {
                            value = parts[1];
                        }

                        _cachedStringDict.Add(new KeyValuePair<string, string>(envKey, value));
                    }
                }
            }
            return _cachedStringDict;
        }
    }
}
