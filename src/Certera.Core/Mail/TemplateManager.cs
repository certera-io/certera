using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Certera.Core.Mail
{
    public static class TemplateManager
    {
        public static string NotificationCertificateAcquisitionFailure { get; private set; }
        public static string NotificationCertificateChange { get; private set; }
        public static string NotificationCertificateExpiration { get; private set; }

        static TemplateManager()
        {
            NotificationCertificateAcquisitionFailure = ReadManifestData("NotificationCertificateAcquisitionFailure.html");
            NotificationCertificateChange = ReadManifestData("NotificationCertificateChange.html");
            NotificationCertificateExpiration = ReadManifestData("NotificationCertificateExpiration.html");
        }

        public static string BuildTemplate(string template, object parameters)
        {
            var pattern = "\\{\\{[A-Za-z0-9]+\\}\\}";
            var parameterValues = parameters.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(x => x.Name, x => x.GetValue(parameters) as string);

            var result = Regex.Replace(template, pattern, 
                match => 
                {
                    var property = match.Value.Substring(2, match.Value.Length - 4);
                    if (parameterValues.TryGetValue(property, out var value))
                    {
                        return value;
                    }
                    return string.Empty;
                }
             );
            return result;
        }

        private static string ReadManifestData(string embeddedFileName)
        {
            var assembly = typeof(TemplateManager).GetTypeInfo().Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .First(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not load manifest resource stream.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
