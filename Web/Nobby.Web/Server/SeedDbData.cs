namespace AspNetCoreSpa.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Nobby.Common;
    using Nobby.Data;
    using Nobby.Data.Models;
    using OpenIddict.Core;
    using OpenIddict.Models;

    public class SeedDbData
    {
        readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public SeedDbData(IWebHost host, ApplicationDbContext context)
        {
            var services = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));
            var serviceScope = services.CreateScope();
            this._hostingEnv = serviceScope.ServiceProvider.GetService<IHostingEnvironment>();
            this._roleManager = serviceScope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
            this._userManager = serviceScope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
            this._context = context;
            this.CreateRoles(); // Add roles
            this.CreateUsers(); // Add users
            this.AddLocalisedData();
            this.AddOpenIdConnectOptions(serviceScope, CancellationToken.None).GetAwaiter().GetResult();
        }

        private void CreateRoles()
        {
            var rolesToAdd = new List<ApplicationRole>(){
                new ApplicationRole { Name= GlobalConstants.AdministratorRoleName},
                new ApplicationRole { Name= GlobalConstants.NormalUserRole}
            };
            foreach (var role in rolesToAdd)
            {
                if (!this._roleManager.RoleExistsAsync(role.Name).Result)
                {
                    this._roleManager.CreateAsync(role).Result.ToString();
                }
            }
        }
        private void CreateUsers()
        {
            if (!this._context.ApplicationUsers.Any())
            {

                this._userManager.CreateAsync(new ApplicationUser { UserName = "admin@admin.com", FirstName = "Admin first", LastName = "Admin last", Email = "admin@admin.com", EmailConfirmed = true }, "P@ssw0rd!").Result.ToString();
                this._userManager.AddToRoleAsync(this._userManager.FindByNameAsync("admin@admin.com").GetAwaiter().GetResult(), GlobalConstants.AdministratorRoleName).Result.ToString();
            }
        }
        private void AddLocalisedData()
        {
            if (!this._context.Cultures.Any())
            {
                this._context.Cultures.AddRange(
                    new Culture
                    {
                        Name = "en-US",
                        Resources = new List<Resource>() {
                            new Resource { Key = "app_title", Value = "AspNetCoreSpa" },
                            new Resource { Key = "app_description", Value = "Single page application using aspnet core and angular" },
                            new Resource { Key = "app_nav_home", Value = "Home" },
                            new Resource { Key = "app_nav_chat", Value = "Chat" },
                            new Resource { Key = "app_nav_examples", Value = "Examples" },
                            new Resource { Key = "app_nav_register", Value = "Register" },
                            new Resource { Key = "app_nav_login", Value = "Login" },
                            new Resource { Key = "app_nav_logout", Value = "Logout" },
                        }
                    }
                    );

                this._context.SaveChanges();
            }

        }

        private async Task AddOpenIdConnectOptions(IServiceScope services, CancellationToken cancellationToken)
        {
            var manager = services.ServiceProvider.GetService<OpenIddictApplicationManager<OpenIddictApplication>>();

            if (await manager.FindByClientIdAsync("aspnetcorespa", cancellationToken) == null)
            {
                var host = this._hostingEnv.IsDevelopment() ? "http://localhost:5000" : "http://aspnetcorespa.azurewebsites.net";
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = "aspnetcorespa",
                    DisplayName = "AspnetCoreSpa",
                    PostLogoutRedirectUris = { new Uri($"{host}/signout-oidc") },
                    RedirectUris = { new Uri(host) }
                    // RedirectUris = { new Uri($"{host}/signin-oidc") }
                };

                await manager.CreateAsync(descriptor, cancellationToken);
            }

            // if (await manager.FindByClientIdAsync("resource-server-1", cancellationToken) == null)
            // {
            //     var descriptor = new OpenIddictApplicationDescriptor
            //     {
            //         ClientId = "resource-server-1",
            //         ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342"
            //     };

            //     await manager.CreateAsync(descriptor, cancellationToken);
            // }

            // if (await manager.FindByClientIdAsync("resource-server-2", cancellationToken) == null)
            // {
            //     var descriptor = new OpenIddictApplicationDescriptor
            //     {
            //         ClientId = "resource-server-2",
            //         ClientSecret = "C744604A-CD05-4092-9CF8-ECB7DC3499A2"
            //     };

            //     await manager.CreateAsync(descriptor, cancellationToken);
            // }

        }

    }
}
