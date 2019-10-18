using DnsClient;
using Certera.Core.Concurrency;
using Certera.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Certera.Web.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Manually disposed")]
    public class DomainScanner
    {
        private readonly Domain _domain;
        private readonly ILookupClient _lookupClient;
        private readonly ILogger _logger;
        private X509Certificate2 _certificate;
        private List<string> _messages = new List<string>();
        private string _scanStatus;

        public DomainScanner(Domain domain, IServiceProvider serviceProvider)
        {
            _domain = domain;
            _lookupClient = serviceProvider.GetService<ILookupClient>();
            _logger = serviceProvider.GetService<ILogger<DomainScanner>>();
        }

        public DomainScanner(Domain domain, ILookupClient lookupClient, ILogger logger)
        {
            _domain = domain;
            _lookupClient = lookupClient;
            _logger = logger;
        }

        public DomainScan Scan()
        {
            return NamedLocker.RunWithLock(_domain.Uri, () =>
            {
                _logger.LogInformation($"Scanning domain {_domain.Uri}");
                var uri = new Uri(_domain.Uri);
                var hostEntry = _lookupClient.GetHostEntry(uri.Host);
                if (hostEntry != null && hostEntry.AddressList.Any())
                {
                    var msg = $"{uri.Host} resolved to the following IP addresses: " +
                        $"{string.Join(", ", hostEntry.AddressList.Select(x => x))}";
                    _messages.Add(msg);

                    _logger.LogDebug(msg);

                    var ip = hostEntry.AddressList.First();
                    OpenSslConnection(uri.Host, ip, uri.Port);
                }
                var domainCert = DomainCertificate.FromX509Certificate2(_certificate, CertificateSource.TrackedDomain);
                _certificate?.Dispose();

                var domainScan = new DomainScan
                {
                    DateScan = DateTime.UtcNow,
                    DomainCertificate = domainCert,
                    ScanResult = string.Join("\r\n", _messages),
                    ScanSuccess = domainCert != null,
                    ScanStatus = _scanStatus
                };
                _logger.LogInformation($"Domain scan finished {(domainScan.ScanSuccess ? "successfully" : "unsuccessfully")} " +
                    $"for {_domain.Uri}");

                return domainScan;
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5397:Do not use deprecated SslProtocols values", Justification = "<Pending>")]
        private void OpenSslConnection(string host, IPAddress ip, int port)
        {
            var tries = 3;
            do
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        if (client.ConnectAsync(ip, port).Wait(5000))
                        {
                            using (var sslStream = new SslStream(
                                client.GetStream(),
                                false,
                                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                                null
                            ))
                            {
                                sslStream.AuthenticateAsClient(host, null,
#pragma warning disable CS0618 // Type or member is obsolete
                            SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore CS0618 // Type or member is obsolete
                            false);
                            }

                            var msg = $"SSL connection established to {host} ({ip}:{port})";

                            _messages.Add(msg);
                            _logger.LogDebug(msg);

                            break;
                        }
                        else
                        {
                            _scanStatus = "connection timeout";
                            var msg = $"SSL connection timeout to {host} ({ip}:{port})";

                            _messages.Add(msg);
                            _logger.LogDebug(msg);
                        }
                    }
                }
                catch (Exception e)
                {
                    var msg = $"Error establishing SSL connection to {host} ({ip}:{port}).";
                    _scanStatus = "error";

                    _messages.Add(msg + " Error: {e.Message}");
                    _logger.LogError(e, msg);
                }
            }
            while (--tries > 0);
        }

        private bool ValidateServerCertificate(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                _certificate = new X509Certificate2(certificate);

                var msg = $"Domain {_domain.Uri}: " +
                    $"{certificate.Subject} " +
                    $"from {certificate.GetEffectiveDateString()} " +
                    $"to {certificate.GetExpirationDateString()} | " +
                    $"issued by {certificate.Issuer} | " +
                    $"policy errors: {sslPolicyErrors.ToString()} | " +
                    $"verify result: {_certificate.Verify()}";

                _messages.Add(msg);
                _logger.LogDebug(msg);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error validating certificate");
                return false;
            }
        }
    }
}
