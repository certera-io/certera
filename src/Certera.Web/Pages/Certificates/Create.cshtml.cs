using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Certera.Data;
using Certera.Data.Models;
using Certera.Web.Services;
using Certes;

namespace Certera.Web.Pages.Certificates
{
    public class CreateModel : PageModel
    {
        private readonly Certera.Data.DataContext _context;
        private readonly KeyGenerator _keyGenerator;

        public CreateModel(Certera.Data.DataContext context, KeyGenerator keyGenerator)
        {
            _context = context;
            _keyGenerator = keyGenerator;
        }

        public IActionResult OnGet()
        {
            LoadData();
            return Page();
        }

        private void LoadData()
        {
            ViewData["AcmeAccountId"] = new SelectList(
                _context.AcmeAccounts.Select(x => new
                {
                    Id = x.AcmeAccountId,
                    Name = x.AcmeContactEmail + (x.IsAcmeStaging ? " (staging)" : string.Empty)
                })
                , "Id", "Name");
        }

        [BindProperty]
        public AcmeCertificate AcmeCertificate { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadData();
                return Page();
            }

            if (AcmeCertificate.KeyId < 0)
            {
                var key = _keyGenerator.Generate(AcmeCertificate.Subject, KeyAlgorithm.RS256);
                if (key == null)
                {
                    ModelState.AddModelError(string.Empty, "Error creating key");
                    return Page();
                }

                AcmeCertificate.KeyId = key.KeyId;
            }

            _context.AcmeCertificates.Add(AcmeCertificate);
            await _context.SaveChangesAsync();

            StatusMessage = "Certificate created";

            return RedirectToPage("./Index");
        }
    }
}