using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSpa.Server.Controllers.api
{
    using Nobby.Data.Models;

    public class AppUtils
    {
        internal static IActionResult SignIn(ApplicationUser user, IList<string> roles)
        {
            var userResult = new { User = new { DisplayName = user.UserName, Roles = roles } };
            return new ObjectResult(userResult);
        }

    }
}