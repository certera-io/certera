﻿using System;
using Nager.PublicSuffix;

namespace Certera.Core.Helpers
{
    public static class DomainParser
    {
        private static WebTldRuleProvider _webTldRuleProvider;
        private static Nager.PublicSuffix.DomainParser _domainParser;

        static DomainParser()
        {
            _webTldRuleProvider = new WebTldRuleProvider(
                cacheProvider: new FileCacheProvider(cacheTimeToLive: TimeSpan.FromDays(7)));
            _domainParser = new Nager.PublicSuffix.DomainParser(_webTldRuleProvider);
        }

        public static string RegistrableDomain(string host)
        {
            if (host == null)
            {
                return null;
            }

            if (host.Contains("*."))
            {
                host = host.Replace("*.", "");
            }

            if (_domainParser.IsValidDomain(host))
            {
                var domain = _domainParser.Parse(host);
                return domain.RegistrableDomain;
            }

            return null;
        }

        public static string GetTld(string fullDomain)
        {
            return _domainParser.Parse(fullDomain).TLD;
        }

        public static string Subdomain(string fullDomain)
        {
            return _domainParser.Parse(fullDomain).SubDomain;
        }
    }
}
