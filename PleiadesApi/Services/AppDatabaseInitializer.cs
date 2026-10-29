using Embix.PgSql;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using Pleiades.Ef.PgSql;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PleiadesApi.Services;

/// <summary>
/// Application database initializer. When the Pleiades database does not
/// exist, this creates it with its schema (including the auth tables and
/// the text index tables), and seeds it from the binary files in the
/// <c>Data:SourceDir</c> directory, if any.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppDatabaseInitializer"/>
/// class.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
public sealed class AppDatabaseInitializer(IServiceProvider serviceProvider) :
    AuthDatabaseInitializer<NamedUser, IdentityRole, NamedSeededUserOptions>(
        serviceProvider)
{
    /// <summary>
    /// Initializes the user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="options">The options.</param>
    protected override void InitUser(NamedUser user,
        NamedSeededUserOptions options)
    {
        base.InitUser(user, options);

        user.FirstName = options.FirstName;
        user.LastName = options.LastName;
    }

    /// <summary>
    /// Initializes the database.
    /// </summary>
    protected override void InitDatabase()
    {
        string name = Configuration.GetValue<string>("DatabaseNames:Data")!;
        Serilog.Log.Information("Checking for database {Name}...", name);

        string csTemplate = Configuration.GetConnectionString("Default")!;
        PgSqlDbManager manager = new(csTemplate);
        if (manager.Exists(name)) return;

        // the Pleiades schema includes the auth tables
        Serilog.Log.Information("Creating database {Name}...", name);
        manager.CreateDatabase(name, PleiadesDbSchema.Get(), null);

        // text index (Embix) tables
        string cs = string.Format(CultureInfo.InvariantCulture, csTemplate, name);
        PgSqlPleiadesTableInitializer indexInitializer =
            new(new PgSqlDbConnectionFactory(cs));
        indexInitializer.Initialize(false);
        Serilog.Log.Information("Database created.");

        // seed data from binary files if present
        string? sourceDir = Configuration.GetValue<string>("Data:SourceDir");
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            Logger?.LogInformation(
                "Data source directory {Directory} not found", sourceDir);
            return;
        }

        Logger?.LogInformation("Seeding Pleiades database from {Directory}",
            sourceDir);
        BulkTablesCopier copier = new(new PgSqlBulkTableCopier(cs));
        copier.Begin();
        copier.Read(sourceDir, CancellationToken.None,
            new Progress<string>(message => Logger?.LogInformation(
                "{Message}", message)));
        copier.End();

        // sync the index identity and refresh statistics
        indexInitializer.CreateIndexes();
        manager.ExecuteCommands(name, "ANALYZE;");
        Logger?.LogInformation("Seeding completed.");
    }
}
