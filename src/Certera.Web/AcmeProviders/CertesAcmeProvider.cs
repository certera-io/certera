using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Certera.Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Certera.Web.AcmeProviders
{
    public class CertesAcmeProvider
    {
        private AcmeContext _acmeContext;
        private Data.Models.AcmeCertificate _acmeCertificate;
        private IOrderContext _order;
        private List<AuthChallengeContainer> _authChallengeContainers;
        private AcmeOrder _acmeOrder;
        private readonly ILogger<CertesAcmeProvider> _logger;

        public CertesAcmeProvider(ILogger<CertesAcmeProvider> logger)
        {
            _logger = logger;
        }

        public async Task<bool> AccountExists(string key, bool staging)
        {
            try
            {
                IKey accountKey = KeyFactory.FromPem(key);

                var acmeContext = new AcmeContext(staging
                    ? WellKnownServers.LetsEncryptStagingV2
                    : WellKnownServers.LetsEncryptV2, accountKey);

                var account = await acmeContext.Account();
                return account != null;
            }
            catch { }
            return false;
        }

        public string NewKey(KeyAlgorithm keyAlgorithm)
        {
            return KeyFactory.NewKey(keyAlgorithm).ToPem();
        }

        public async Task CreateAccount(string email, string key, bool staging)
        {
            var accountKey = KeyFactory.FromPem(key);

            var acmeContext = new AcmeContext(staging
                ? WellKnownServers.LetsEncryptStagingV2
                : WellKnownServers.LetsEncryptV2, accountKey);

            await acmeContext.NewAccount(email, true);
        }

        public async Task<string> CreateNewAccount(string email, bool staging)
        {
            var acmeContext = new AcmeContext(staging
                ? WellKnownServers.LetsEncryptStagingV2
                : WellKnownServers.LetsEncryptV2);

            await acmeContext.NewAccount(email, true);

            var keyPem = acmeContext.AccountKey.ToPem();
            return keyPem;
        }

        public void Initialize(Data.Models.AcmeCertificate acmeCert)
        {
            _acmeCertificate = acmeCert;

            IKey accountKey = KeyFactory.FromPem(acmeCert.AcmeAccount.Key.RawData);

            _acmeContext = new AcmeContext(acmeCert.AcmeAccount.IsAcmeStaging
                ? WellKnownServers.LetsEncryptStagingV2
                : WellKnownServers.LetsEncryptV2, accountKey);
        }

        public async Task<AcmeOrder> BeginOrder()
        {
            var domains = new List<string> { _acmeCertificate.Subject };

            if (!string.IsNullOrWhiteSpace(_acmeCertificate.SANs))
            {
                var sans = _acmeCertificate.SANs
                    .Split(new[] { "\r\n", "\r", "\n", ",", ";" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                domains.AddRange(sans);
            }

            _acmeOrder = new AcmeOrder
            {
                AcmeCertificate = _acmeCertificate,
                DateCreated = DateTime.UtcNow,
                Status = AcmeOrderStatus.Created
            };

            _acmeCertificate.AcmeOrders.Add(_acmeOrder);

            try
            {
                _order = await _acmeContext.NewOrder(domains);
                _logger.LogDebug($"Order created: {_order.Location}");

                // Get authorizations for the new order which we'll then place
                var authz = await _order.Authorizations();

                // Track all auth requests to the corresponding validation and 
                // subsequent completion and certificate response
                _authChallengeContainers = authz.Select(x => new AuthChallengeContainer
                {
                    AuthorizationContext = x,

                    // TODO: Once dns-01 is implemented, check and specify the type below
                    ChallengeContextTask = x.Http()
                }).ToList();

                await Task.WhenAll(_authChallengeContainers.Select(x => x.ChallengeContextTask).ToList());

                _acmeOrder.Status = AcmeOrderStatus.Challenging;

                foreach (var cc in _authChallengeContainers)
                {
                    cc.ChallengeContext = cc.ChallengeContextTask.Result;
                    var acmeReq = new AcmeRequest
                    {
                        KeyAuthorization = cc.ChallengeContext.KeyAuthz,
                        Token = cc.ChallengeContext.Token,
                        DateCreated = DateTime.UtcNow,
                        AcmeOrder = _acmeOrder
                    };
                    _acmeOrder.AcmeRequests.Add(acmeReq);
                    cc.AcmeRequest = acmeReq;
                }
            }
            catch (AcmeRequestException e)
            {
                _logger.LogError(e, "Error requesting order");
                _acmeOrder.Status = AcmeOrderStatus.Error;
                _acmeOrder.Errors = e.Message;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating order");
                _acmeOrder.Status = AcmeOrderStatus.Error;
            }

            return _acmeOrder;
        }

        public async Task<AcmeOrder> Validate()
        {
            if (_acmeOrder.Status != AcmeOrderStatus.Challenging)
            {
                return _acmeOrder;
            }
            try
            {
                foreach (var cc in _authChallengeContainers)
                {
                    cc.ChallengeTask = cc.ChallengeContext.Validate();
                }

                await Task.WhenAll(_authChallengeContainers.Select(x => x.ChallengeTask).ToList());

                foreach (var cc in _authChallengeContainers)
                {
                    cc.Challenge = cc.ChallengeTask.Result;
                }

                _acmeOrder.Status = AcmeOrderStatus.Validating;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error validating order");
                _acmeOrder.Status = AcmeOrderStatus.Error;
            }
            return _acmeOrder;
        }

        public async Task<AcmeOrder> Complete()
        {
            if (_acmeOrder.Status != AcmeOrderStatus.Validating)
            {
                return _acmeOrder;
            }

            foreach (var cc in _authChallengeContainers)
            {
                cc.AuthorizationTask = cc.AuthorizationContext.Resource();
            }

            var attempts = 5;
            do
            {
                // Kick off the authorization tasks for the tasks that haven't been run yet
                await Task.WhenAll(_authChallengeContainers
                    .Where(x => !x.AuthorizationTask.IsCompleted)
                    .Select(x => x.AuthorizationTask)
                    .ToList());

                var incompletes = 0;
                // After running the tasks, find all incomplete authz
                foreach (var cc in _authChallengeContainers)
                {
                    var status = cc.AuthorizationTask.Result.Status;
                    var completed = status == AuthorizationStatus.Valid || 
                                    status == AuthorizationStatus.Invalid;
                    if (!completed)
                    {
                        incompletes++;

                        // Update the task such that it's a new task and it will be awaited above
                        cc.AuthorizationTask = cc.AuthorizationContext.Resource();
                    }
                    else
                    {
                        cc.Authorization = cc.AuthorizationTask.Result;
                    }
                }

                // Find incomplete ones and try again
                _logger.LogDebug($"{incompletes} incomplete authorizations.");

                if (incompletes == 0)
                {
                    break;
                }

                await Task.Delay(5000);

            } while (attempts-- > 0);

            // All authorizations have completed, save the results
            foreach (var cc in _authChallengeContainers)
            {
                cc.Authorization = cc.AuthorizationTask.Result;
            }
            
            // At this point, they're all complete and need to see which are valid/invalid
            // and obtain the cert if possible.
            try
            {
                var invalidResp = _authChallengeContainers
                    .SelectMany(x => x.Authorization.Challenges)
                    .Where(x => x.Error != null)
                    .ToList();

                var errors = string.Join("\r\n", invalidResp
                    .Select(x => $"{x.Error.Status} {x.Error.Type} {x.Error.Detail}"));

                _acmeOrder.RequestCount = _authChallengeContainers.Count;
                _acmeOrder.InvalidResponseCount = invalidResp.Count;
                _acmeOrder.Errors = errors;
                _acmeOrder.Status = AcmeOrderStatus.Completed;

                if (invalidResp.Count > 0)
                {
                    _acmeOrder.Status = AcmeOrderStatus.Invalid;
                    return _acmeOrder;
                }

                var cert = await _order.Generate(
                    new CsrInfo
                    {
                        CommonName = _acmeCertificate.CsrCommonName,
                        CountryName = _acmeCertificate.CsrCountryName,
                        Locality = _acmeCertificate.CsrLocality,
                        Organization = _acmeCertificate.CsrOrganization,
                        OrganizationUnit = _acmeCertificate.CsrOrganizationUnit,
                        State = _acmeCertificate.CsrState
                    }, KeyFactory.FromPem(_acmeCertificate.Key.RawData));

                var certBytes = cert.Certificate.ToDer();

                var xCert = new X509Certificate2(certBytes);
                var domainCert = DomainCertificate.FromX509Certificate2(xCert, CertificateSource.AcmeCertificate);
                xCert.Dispose();

                _acmeOrder.RawDataPem = cert.ToPem();
                _acmeOrder.DomainCertificate = domainCert;

                return _acmeOrder;
            }
            catch (AcmeRequestException e)
            {
                _acmeOrder.Status = AcmeOrderStatus.Error;
                _acmeOrder.Errors = $"{e.Error.Status} {e.Error.Type} {e.Error.Detail}";
            }
            catch (Exception)
            {
                _acmeOrder.Status = AcmeOrderStatus.Error;
                _acmeOrder.Errors = "Unknown Error";
            }
            return _acmeOrder;
        }
    }

    public class AuthChallengeContainer
    {
        public AcmeRequest AcmeRequest { get; set; }
        public IAuthorizationContext AuthorizationContext { get; set; }
        public Task<IChallengeContext> ChallengeContextTask { get; set; }
        public IChallengeContext ChallengeContext { get; set; }
        public Task<Challenge> ChallengeTask { get; set; }
        public Challenge Challenge { get; set; }
        public Task<Authorization> AuthorizationTask { get; set; }
        public Authorization Authorization { get; set; }
    }
}
