/* Code taken and modified from https://github.com/win-acme/win-acme/blob/master/src/main.lib/Plugins/ValidationPlugins/Dns/DnsValidation.cs
 * Win-ACME is licensed as Apache License 2.0
 * Modifications:
 *   Kept overall logic, but changed things to operate without needing addtional services and frameworks used by win-acme.
*/
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Certera.Web.Services.Dns
{
    public class DnsVerifier
    {
        private const int MaxRetries = 5;
        private const int RetrySeconds = 30;

        private readonly ILogger<DnsVerifier> _logger;
        private readonly LookupClientProvider _lookupClientProvider;

        public DnsVerifier(ILogger<DnsVerifier> logger, LookupClientProvider lookupClientProvider)
        {
            _logger = logger;
            _lookupClientProvider = lookupClientProvider;
        }

        public async Task PreValidate(string dnsRecord, string value)
        {
            var attempt = 0;

            while (true)
            {
                if (await EnsureDnsRecordValue(dnsRecord, value, attempt))
                {
                    break;
                }
                else
                {
                    attempt += 1;
                    if (attempt > MaxRetries)
                    {
                        _logger.LogInformation("DNS self check failed.");
                        break;
                    }
                    else
                    {
                        _logger.LogInformation($"Will retry in {RetrySeconds} seconds (retry {attempt}/{MaxRetries})...");
                        await Task.Delay(RetrySeconds * 1000);
                    }
                }
            }
        }

        private async Task<bool> EnsureDnsRecordValue(string dnsRecord, string value, int attempt)
        {
            try
            {
                var dnsClients = await _lookupClientProvider.GetClients(dnsRecord, attempt);
                foreach (var client in dnsClients)
                {
                    _logger.LogDebug($"Preliminary validation will now check name server {client.IpAddress}");
                    var answers = await client.GetTextRecordValues(dnsRecord, attempt);
                    if (!answers.Any())
                    {
                        _logger.LogWarning($"Preliminary validation at {client.IpAddress} failed: no TXT records found");
                        return false;
                    }
                    if (!answers.Contains(value))
                    {
                        _logger.LogWarning($"Preliminary validation at {client.IpAddress} failed: {value} not found in {string.Join(", ", answers)}");
                        return false;
                    }
                    _logger.LogDebug($"Preliminary validation at {client.IpAddress} looks good!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Preliminary validation failed");
                return false;
            }
            _logger.LogInformation("Preliminary validation succeeded");
            return true;
        }
    }
}
