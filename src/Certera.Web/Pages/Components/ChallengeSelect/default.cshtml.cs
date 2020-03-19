using Certera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Certera.Web.Pages.Components.ChallengeSelect
{
    public class ChallengeSelectViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public ChallengeSelectViewComponent(DataContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(string name, string selected = null)
        {
            var setScript = _context.GetSetting<string>(Data.Settings.Dns01SetScript, null);
            var cleanupScript = _context.GetSetting<string>(Data.Settings.Dns01CleanupScript, null);

            var disabled = false;

            if (string.IsNullOrWhiteSpace(setScript) || string.IsNullOrWhiteSpace(cleanupScript))
            {
                disabled = true;
                selected = "http-01";
            }

            var selectListItems = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "HTTP-01",
                    Value = "http-01",
                    Disabled = disabled
                },
                new SelectListItem
                {
                    Text = "DNS-01",
                    Value = "dns-01",
                    Disabled = disabled
                },
            };

            foreach (var item in selectListItems)
            {
                if (string.Equals(item.Value, selected, System.StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    break;
                }
            }

            return View(new ChallengeSelectModel { Name = name, Items = selectListItems });
        }
    }

    public class ChallengeSelectModel
    {
        public string Name { get; set; }
        public List<SelectListItem> Items { get; set; }
    }
}