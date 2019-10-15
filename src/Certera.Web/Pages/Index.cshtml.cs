using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Certera.Web.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // There's nothing on the root page (yet). Reserve it so we can maybe have a dashboard or something.
            HttpContext.Response.Redirect("/tracking");
        }
    }
}
