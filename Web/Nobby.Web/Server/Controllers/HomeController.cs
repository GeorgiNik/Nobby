// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AspNetCoreSpa.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using AspNetCoreSpa.Server.ViewModels;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Localization;
    using Nobby.Data.Models;

    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly IHostingEnvironment _env;
        private readonly IStringLocalizer<HomeController> _stringLocalizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            IHostingEnvironment env,
            IStringLocalizer<HomeController> stringLocalizer,
            IMemoryCache memoryCache
        )
        {
            this._userManager = userManager;
            this._stringLocalizer = stringLocalizer;
            this._env = env;
            this._cache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
            if (this.ConfirmEmailRequest())
            {
                await this.ConfirmEmail();
            }

            string content = this.GetContentByCulture();

            this.ViewBag.content = content;

            return this.View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture)
        {
            this.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                                         CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)), new CookieOptions
                                         {
                                             Expires = DateTimeOffset.UtcNow.AddYears(1)
                                         }
                                        );

            return this.LocalRedirect("~/");
        }

        private bool ConfirmEmailRequest()
        {
            return this.Request.Query.ContainsKey("emailConfirmCode") && this.Request.Query.ContainsKey("userId");
        }

        private async Task ConfirmEmail()
        {
            string userId = this.Request.Query["userId"].ToString();
            string code = this.Request.Query["emailConfirmCode"].ToString();
            code = code.Replace(" ", "+");

            ApplicationUser user = await this._userManager.FindByIdAsync(userId);
            if (user != null && !user.EmailConfirmed)
            {
                IdentityResult valid = await this._userManager.ConfirmEmailAsync(user, code);
                if (valid.Succeeded)
                {
                    this.ViewBag.emailConfirmed = true;
                }
            }
        }

        private string GetContentByCulture()
        {
            var requestCulture = this.HttpContext.Features.Get<IRequestCultureFeature>();
            // Culture contains the information of the requested culture
            CultureInfo culture = requestCulture.RequestCulture.Culture;

            string CACHE_KEY = $"Content-{culture.Name}";

            string cacheEntry;

            // Look for cache key.
            if (!this._cache.TryGetValue(CACHE_KEY, out cacheEntry))
            {
                // Key not in cache, so get & set data.
                Dictionary<string, string> culturalContent = this._stringLocalizer.WithCulture(culture).GetAllStrings().Select(c => new ContentVm
                {
                    Key = c.Name,
                    Value = c.Value
                }).ToDictionary(x => x.Key, x => x.Value);
                cacheEntry = Helpers.JsonSerialize(culturalContent);

                // Set cache options.
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                // Save data in cache.
                this._cache.Set(CACHE_KEY, cacheEntry, cacheEntryOptions);
            }

            return cacheEntry;
        }
    }
}