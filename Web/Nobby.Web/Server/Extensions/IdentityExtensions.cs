using System;
using System.Security.Claims;
using AspNet.Security.OpenIdConnect.Primitives;

namespace AspNetCoreSpa.Server.Extensions
{
    public static class IdentityExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            string id = principal.FindFirst(OpenIdConnectConstants.Claims.Subject)?.Value;

            return id;
        }
    }
}
