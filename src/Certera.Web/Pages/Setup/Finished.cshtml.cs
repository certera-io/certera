using Certera.Web.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Certera.Web.Pages.Setup
{
    public class FinishedModel : PageModel
    {
        private readonly IWritableOptions<Options.Setup> _setupOptions;

        public FinishedModel(IWritableOptions<Options.Setup> setupOptions)
        {
            _setupOptions = setupOptions;
        }

        public void OnGet()
        {
            _setupOptions.Update(x => x.Finished = true);
        }
    }
}