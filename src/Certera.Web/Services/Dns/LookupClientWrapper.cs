/* Code taken and modified from https://github.com/win-acme/win-acme/blob/master/src/main.lib/Clients/DNS/LookupClientWrapper.cs
 * Win-ACME is licensed as Apache License 2.0
 * Modifications:
 *   Kept overall logic, but changed things to operate without needing addtional services and frameworks used by win-acme.
*/
using DnsClient;
using DnsClient.Protocol;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Certera.Web.Services.Dns
{
    public class LookupClientWrapper
    {
        private readonly LookupClientProvider _provider;
        private readonly IPAddress _ipAddress;

        public ILookupClient LookupClient { get; private set; }
        public string IpAddress => _ipAddress.ToString();

        public LookupClientWrapper(IPAddress ipAddress, LookupClientProvider provider)
        {
            _ipAddress = ipAddress;
            LookupClient = new LookupClient(ipAddress);
            LookupClient.UseCache = false;
            _provider = provider;
        }

        public async Task<IEnumerable<IPAddress>> GetAuthoritativeNameServers(string domainName, int round)
        {
            domainName = domainName.TrimEnd('.');
            var nsResponse = await LookupClient.QueryAsync(domainName, QueryType.NS);
            var nsRecords = nsResponse.Answers.NsRecords();
            var cnameRecords = nsResponse.Answers.CnameRecords();
            if (!nsRecords.Any() && !cnameRecords.Any())
            {
                nsRecords = nsResponse.Authorities.OfType<NsRecord>();
            }
            if (nsRecords.Any())
            {
                return GetNameServerIpAddresses(nsRecords.Select(n => n.NSDName.Value), round);
            }
            return null;
        }

        private IEnumerable<IPAddress> GetNameServerIpAddresses(IEnumerable<string> nsRecords, int round)
        {
            foreach (var nsRecord in nsRecords)
            {
                var aResponse = _provider.GetDefaultClient(round).LookupClient.Query(nsRecord, QueryType.A);
                var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                if (nameServerIp != null)
                {
                    yield return nameServerIp;
                }
            }
        }

        public async Task<IEnumerable<string>> GetTextRecordValues(string challengeUri, int attempt)
        {
            var result = await LookupClient.QueryAsync(challengeUri, QueryType.TXT);
            result = await RecursivelyFollowCnames(result, attempt);

            return result.Answers.TxtRecords().
                Select(txtRecord => txtRecord?.EscapedText?.FirstOrDefault()).
                Where(txtRecord => txtRecord != null).
                OfType<string>().
                ToList();
        }

        private async Task<IDnsQueryResponse> RecursivelyFollowCnames(IDnsQueryResponse result, int attempt)
        {
            if (result.Answers.CnameRecords().Any())
            {
                var cname = result.Answers.CnameRecords().First();
                var recursiveClients = await _provider.GetClients(cname.CanonicalName, attempt);
                var index = attempt % recursiveClients.Count;
                var recursiveClient = recursiveClients.ElementAt(index);
                var txtResponse = await recursiveClient.LookupClient.QueryAsync(cname.CanonicalName, QueryType.TXT);
                return await recursiveClient.RecursivelyFollowCnames(txtResponse, attempt);
            }
            return result;
        }
    }
}