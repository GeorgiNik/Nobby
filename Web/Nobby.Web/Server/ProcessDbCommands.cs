namespace AspNetCoreSpa.Server
{
    using System;
    using AspNetCoreSpa.Server.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Nobby.Data;

    public class ProcessDbCommands
    {
        public static void Process(string[] args, IWebHost host)
        {
            var services = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));
            var seedService = (SeedDbData)host.Services.GetService(typeof(SeedDbData));

            using (IServiceScope scope = services.CreateScope())
            {
                ApplicationDbContext db = GetApplicationDbContext(scope);
                // if (args.Contains("dropdb"))
                // {
                //     Console.WriteLine("Dropping database");
                //     db.Database.EnsureDeleted();
                // }

                // if (args.Contains("migratedb"))
                // {
                // Console.WriteLine("Migrating database");
                // db.Database.Migrate();
                // }

                // if (args.Contains("seeddb"))
                // {
                Console.WriteLine("Seeding database");
                db.Seed(host);
                // }
            }
        }

        private static ApplicationDbContext GetApplicationDbContext(IServiceScope services)
        {
            var db = services.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return db;
        }
    }
}