﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using SimpleIdServer.Scim.Persistence.EF;

namespace SimpleIdServer.IdServer.PostgreMigrations
{
    /*
    public class DbContextFactory : IDesignTimeDbContextFactory<SCIMDbContext>
    {
        public SCIMDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SCIMDbContext>();
            var efOptions = Options.Create(new ScimEfOptions());
            var connectionString = "server=localhost;port=3306;database=idserver;user=root;password=password";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), o =>
            {
                o.MigrationsAssembly("SimpleIdServer.Scim.MySQLMigrations");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            return new SCIMDbContext(optionsBuilder.Options, efOptions);
        }
    }
    */
}

namespace SimpleIdServer.Scim.SqliteMigrations
{
    // Uncomment the class below to be able to create a new database migration class for Sqlite
    /*
    public class SCIMDbContextFactory : IDesignTimeDbContextFactory<SCIMDbContext>
    {
        public SCIMDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SCIMDbContext>();
            optionsBuilder.UseSqlite("Data Source=scim.db", o =>
                o.MigrationsAssembly("SimpleIdServer.Scim.SqliteMigrations"));

            return new SCIMDbContext(
                dbContextOptions: optionsBuilder.Options,
                options: new OptionsWrapper<ScimEfOptions>(new ScimEfOptions { DefaultSchema = "scim" }));
        }
    }
    */
}
