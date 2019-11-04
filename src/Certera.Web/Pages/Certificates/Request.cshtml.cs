using Certera.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Certera.Web.Pages.Certificates
{
    public class RequestModel : PageModel
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RequestModel(IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory)
        {
            _queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult OnGet(long? id = null, string returnUrl = null)
        {
            if (id != null)
            {
                _queue.QueueBackgroundWorkItem(async token =>
                {
                    var localId = id.Value;

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var acquirer = scope.ServiceProvider.GetService<CertificateAcquirer>();
                        await acquirer.AcquireAcmeCert(localId, userRequested: true);
                    }
                });
                StatusMessage = "Certificate requested";
            }

            return new RedirectResult(returnUrl ?? Url.Page("./Index"));
        }
    }
}