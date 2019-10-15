using Certes;
using Certera.Data;
using Certera.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Certera.Web.Controllers
{
    [ApiKeyAuthorize]
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