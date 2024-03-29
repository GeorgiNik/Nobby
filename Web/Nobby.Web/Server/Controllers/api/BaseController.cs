using AspNetCoreSpa.Server.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSpa.Server.Controllers.api
{
    using Nobby.Data.Models;

    [Authorize]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class BaseController : Controller
    {
        public BaseController()
        {
        }
        public IActionResult Render(ExternalLoginStatus status)
        {
            return RedirectToAction("Index", "Home", new { externalLoginStatus = (int)status });
        }
    }


}
