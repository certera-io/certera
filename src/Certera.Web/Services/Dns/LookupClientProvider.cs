/* Code taken and modified from https://github.com/win-acme/win-acme/blob/master/src/main.lib/Clients/DNS/LookupClientProvider.cs
 * Win-ACME is licensed as Apache License 2.0
 * Modifications:
 *   Kept overall logic, but changed things to operate without needing addtional services and frameworks used by win-acme.
*/
using Certera.Core.Helpers;
using Certera.Web.Options;
using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Certera.Web.Services.Dns
{
    public class LookupClientProvider
    {
        private readonly List<IPAddress> _defaultNs;
        private readonly Dictionary<string, IEnumerable<IPAddress>> _authoritativeNs;
        private readonly Dictionary<string, LookupClientWrapper> _lookupClients;

        private readonly ILogger<LookupClientProvider> _logger;
        private readonly IOptionsSnapshot<DnsServers> _dnsOptions;

        public LookupClientProvider(ILogger<LookupClientProvider> logger, IOptionsSnapshot<DnsServers> dnsOptions)
        {
            _logger = logger;
            _dnsOptions = dnsOptions;
            _authoritativeNs = new Dictionary<string, IEnumerable<IPAddress>>();
            _lookupClients = new Dictionary<string, LookupClientWrapper>();
            _defaultNs = new List<IPAddress>();

            var ips = _dnsOptions.Value?.IPs ?? new[] { "1.1.1.1", "8.8.8.8", "4.4.4.4" };
            foreach (var ip in ips)
            {
                if (IPAddress.TryParse(ip, out var addr))
                {
                    _defaultNs.Add(addr);
                }
            }
        }

        /// <summary>
        /// Produce a new LookupClientWrapper or take a previously
        /// cached one from the dictionary
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private LookupClientWrapper Produce(IPAddress ip)
        {
            if (ip == null)
            {
                return null;
            }
            var key = ip.ToString();
            if (!_lookupClients.ContainsKey(key))
            {
                _lookupClients.Add(
                    key,
                    new LookupClientWrapper(
                        ip.Equals(new IPAddress(0)) ? null : ip,
                        this));
            }
            return _lookupClients[key];
        }

        private List<LookupClientWrapper> _lookupClientWrappers;
        /// <summary>
        /// Get clients for all default DNS servers
        /// </summary>
        /// <returns></returns>
        public List<LookupClientWrapper> GetDefaultClients()
        {
            if (_lookupClientWrappers == null)
            {
                _lookupClientWrappers = _defaultNs
                    .Select(x => Produce(x))
                    .Where(x => x != null)
                    .ToList();
            }
            return _lookupClientWrappers;
        }

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public LookupClientWrapper GetDefaultClient(int round)
        {
            var index = round % GetDefaultClients().Count;
            var ret = GetDefaultClients().ElementAt(index);
            return ret;
        }

        /// <summary>
        /// Get cached list of authoritative name server ip addresses
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IPAddress>> GetAuthoritativeNameServersForDomain(string domainName, int round)
        {
            var key = domainName.ToLower().TrimEnd('.');
            if (!_authoritativeNs.ContainsKey(key))
            {
                try
                {
                    // _acme-challenge.sub.example.co.uk
                    domainName = domainName.TrimEnd('.');

                    // First domain we should try to ask 
                    var rootDomain = DomainParser.GetTld(domainName);
                    var testZone = rootDomain;
                    var client = GetDefaultClient(round);

                    // Other sub domains we should try asking:
                    // 1. sub
                    // 2. _acme-challenge
                    var remainingParts = domainName.Substring(0, domainName.LastIndexOf(rootDomain))
                        .Trim('.').Split('.')
                        .Where(x => !string.IsNullOrEmpty(x));
                    remainingParts = remainingParts.Reverse();

                    var digDeeper = true;
                    IEnumerable<IPAddress>? ipSet = null;
                    do
                    {
                        // Partial result caching
                        if (!_authoritativeNs.ContainsKey(testZone))
                        {
                            _logger.LogDebug($"Querying server {client.IpAddress} about {testZone}");

                            var tempResult = await client.GetAuthoritativeNameServers(testZone, round);
                            _authoritativeNs.Add(testZone, tempResult?.ToList() ?? ipSet ?? _defaultNs);
                        }
                        ipSet = _authoritativeNs[testZone];
                        client = Produce(ipSet.OrderBy(x => Guid.NewGuid()).First());
                        if (remainingParts.Any())
                        {
                            testZone = $"{remainingParts.First()}.{testZone}";
                            remainingParts = remainingParts.Skip(1).ToArray();
                        }
                        else
                        {
                            digDeeper = false;
                        }
                    }
                    while (digDeeper);

                    if (ipSet == null)
                    {
                        throw new Exception("No results");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Unable to find or contact authoritative name servers for {domainName}: {ex.Message}");
                    _authoritativeNs.Add(key, _defaultNs);
                }
            }

            return _authoritativeNs[key];
        }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by domainName.
        /// Use <see cref="DefaultClient"/> instead if a name server
        /// for a specific domain name is not required.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using a name
        /// server associated with the specified domain name.</returns>
        public async Task<List<LookupClientWrapper>> GetClients(string domainName, int round = 0)
        {
            var ipSet = await GetAuthoritativeNameServersForDomain(domainName, round);
            return ipSet.Select(ip => Produce(ip)).Where(x => x != null).ToList();
        }

    }
}