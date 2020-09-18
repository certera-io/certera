using System;
using System.Collections.Generic;
using System.Text;

namespace Certera.Core.Helpers
{
    public static class EnvironmentVariableHelper
    {
        public static IDictionary<string, string> ToKeyValuePair(string envVars)
        {
            var result = new Dictionary<string, string>();
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

                        result.Add(envKey, value);
                    }
                }
            }
            return result;
        }
    }
}
