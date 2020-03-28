using System;
using System.Collections.Generic;
using System.Text;

namespace Certera.Core.Helpers
{
    public static class EnvironmentVariableHelper
    {
        /// <summary>
        /// Adds trailing backslash to be added in front of a command.
        /// i.e. the following
        /// VAR1=ABC
        /// VAR2=DEF
        /// 
        /// becomes:
        /// VAR1=ABC \
        /// VAR2=DEF \
        /// 
        /// With a newline at end so commands can easily be appended
        /// </summary>
        public static string ToNixEnvVars(string envVars)
        {
            var formatted = envVars;
            if (!string.IsNullOrWhiteSpace(envVars))
            {
                var sb = new StringBuilder();
                var lines = envVars.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    sb.Append(line);
                    var trimmed = line.TrimEnd();
                    if (!trimmed.EndsWith("\\"))
                    {
                        sb.Append(" \\");
                    }
                    sb.AppendLine();
                }
                formatted = sb.ToString();
            }
            return formatted;
        }

        public static List<KeyValuePair<string, string>> ToKeyValuePair(string envVars)
        {
            var kvp = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(envVars))
            {
                var lines = envVars.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
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

                        kvp.Add(new KeyValuePair<string, string>(envKey, value));
                    }
                }
            }
            return kvp;
        }
    }
}
