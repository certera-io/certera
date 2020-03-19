using Certera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Certera.Web.Pages.Components.KeySelect
{
    public class KeySelectViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public KeySelectViewComponent(DataContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(string name, long? selected = null, bool? hideGenNewKeyOption = false)
        {
            var keysList = new SelectList(_context.Keys, "KeyId", "Name");
            var newKeyList = new List<SelectListItem>();
            if (hideGenNewKeyOption == null || hideGenNewKeyOption.Value == false)
            {
                newKeyList.Add(new SelectListItem { Text = "Generate & re-use new key", Value = "-1" });
            }
            newKeyList.AddRange(keysList);
            foreach (var item in newKeyList)
            {
                if (item.Value == selected?.ToString())
                {
                    item.Selected = true;
                    break;
                }
            }
            return View(new KeySelectModel { Name = name, Items = newKeyList });
        }
    }

    public class KeySelectModel
    {
        public string Name { get; set; }
        public List<SelectListItem> Items { get; set; }
    }
}