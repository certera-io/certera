using Certes;
using Certera.Data;
using Certera.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Certera.Web.Controllers
{
    [KeyApiKeyAuthorize]
    [Route("api/[controller]")]
    public class KeyController : Controller
    {
        private readonly DataContext _dataContext;

        public KeyController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Index(string name,
            string format = "pem")
        {
            var key = await _dataContext.Keys
                .FirstOrDefaultAsync(x => x.Name == name);

            if (key == null)
            {
                return NotFound("Key with that name does not exist");
            }

            // Ensure key matches the one used during authentication
            var id = User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.Equals(key.KeyId.ToString(), id))
            {
                return StatusCode(403, "Status Code: 403; Forbidden");
            }

            switch (format?.ToLower() ?? "pem")
            {
                case "der":
                    var ikey = KeyFactory.FromPem(key.RawData);
                    return new ContentResult
                    {
                        Content = Convert.ToBase64String(ikey.ToDer()),
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
                case "pem":
                default:
                    return new ContentResult
                    {
                        Content = key.RawData,
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
            }
        }
    }
}