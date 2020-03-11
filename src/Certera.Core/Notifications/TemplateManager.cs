using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Certera.Core.Notifications
{
    public static class TemplateManager
    {
        public static string NotificationCertificateAcquisitionFailureEmail { get; private set; }
        public static string NotificationCertificateAcquisitionFailureSlack { get; private set; }
        public static string NotificationCertificateChangeEmail { get; private set; }
        public static string NotificationCertificateChangeSlack { get; private set; }
        public static string NotificationCertificateExpirationEmail { get; private set; }
        public static string NotificationCertificateExpirationSlack { get; private set; }

        static TemplateManager()
        {
            NotificationCertificateAcquisitionFailureEmail = ReadManifestData("NotificationCertificateAcquisitionFailureEmail.html");
            NotificationCertificateAcquisitionFailureSlack = ReadManifestData("NotificationCertificateAcquisitionFailureSlack.json");
            NotificationCertificateChangeEmail = ReadManifestData("NotificationCertificateChangeEmail.html");
            NotificationCertificateChangeSlack = ReadManifestData("NotificationCertificateChangeSlack.json");
            NotificationCertificateExpirationEmail = ReadManifestData("NotificationCertificateExpirationEmail.html");
            NotificationCertificateExpirationSlack = ReadManifestData("NotificationCertificateExpirationSlack.json");
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
