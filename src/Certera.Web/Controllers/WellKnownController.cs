using Certera.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Certera.Web.Controllers
{
    public class WellKnownController : Controller
    {
        private readonly DataContext _dataContext;

        public WellKnownController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet(".well-known/acme-challenge/{id}")]
        public IActionResult AcmeChallenge(string id)
        {
            var acmeReq = _dataContext.AcmeRequests.FirstOrDefault(x => x.Token == id);
            if (acmeReq == null)
            {
                return NotFound();
            }

            return new ContentResult
            {
                StatusCode = 200,
                ContentType = "text/plain",
                Content = acmeReq.KeyAuthorization
            };
        }
    }
}