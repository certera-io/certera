using Certera.Data;
using Certera.Data.Models;
using Certera.Web.Extensions;
using Certera.Web.Services;
using Certes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Certera.Web.Pages.Keys
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
            return Page();
        }

        [BindProperty]
        public Key Key { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnPostAsync(IFormFile keyFile, int keyAlgorithm)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            IKey key = null;
            // First check the plain text uploaded PEM encoded certificate (if any)
            if (!string.IsNullOrWhiteSpace(Key.RawData))
            {
                try
                {
                    key = KeyFactory.FromPem(Key.RawData);
                }
                catch (Exception) { }
            }

            // If key is still null, check the uploaded key contents
            if (key == null)
            {
                var keyFileContents = await keyFile.ReadAsBytesAsync();
                if (keyFileContents != null)
                {
                    // Check if it's DER encoded
                    try
                    {
                        key = KeyFactory.FromDer(keyFileContents);
                    }
                    catch (Exception) { }

                    if (key == null)
                    {
                        // How about PEM?
                        var keyPem = Encoding.UTF8.GetString(keyFileContents);

                        try
                        {
                            key = KeyFactory.FromPem(keyPem);
                        }
                        catch (Exception) { }
                    }

                    if (key != null)
                    {
                        Key.RawData = key.ToPem();
                    }
                }                
            }
            var validKeyAlgValues = (int[])Enum.GetValues(typeof(KeyAlgorithm));

            if (key == null && validKeyAlgValues.Contains(keyAlgorithm))
            {
                var keyAlg = (KeyAlgorithm)keyAlgorithm;
                _keyGenerator.Generate(Key.Name, keyAlg, Key.Description);
                return RedirectToPage("./Index");
            }

            if (key == null)
            {
                ModelState.AddModelError(string.Empty, "You must pick a key algorithm, enter in the PEM or upload a key");
                return Page();
            }

            Key.ApiKey1 = ApiKeyGenerator.CreateApiKey();
            Key.ApiKey2 = ApiKeyGenerator.CreateApiKey();

            _context.Keys.Add(Key);
            await _context.SaveChangesAsync();

            StatusMessage = "Key created";

            return RedirectToPage("./Index");
        }
    }
}