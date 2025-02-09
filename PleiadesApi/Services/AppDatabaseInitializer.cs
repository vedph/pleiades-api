﻿using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using PleiadesApi.Models;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PleiadesApi.Services;

/// <summary>
/// Application database initializer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppDatabaseInitializer"/>
/// class.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
public sealed class AppDatabaseInitializer :
    AuthDatabaseInitializer<NamedUser, IdentityRole, NamedSeededUserOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDatabaseInitializer"/>
    /// class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public AppDatabaseInitializer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

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

    private static string LoadResourceText(string name)
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"PleiadesApi.Assets.{name}")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Initializes the database.
    /// </summary>
    protected override void InitDatabase()
    {
        // check if DB exists
        string name = Configuration.GetValue<string>("DatabaseNames:Data")!;
        Serilog.Log.Information("Checking for database {Name}...", name);

        string csTemplate = Configuration.GetConnectionString("Default")!;
        PgSqlDbManager manager = new(csTemplate);

        if (!manager.Exists(name))
        {
            Serilog.Log.Information("Creating database {Name}...", name);

            manager.CreateDatabase(name, PleiadesDbSchema.Get(), null);
            Serilog.Log.Information("Database created.");

            // we need to add to the generic Pleiades schema the auth tables,
            // plus the text index (Embix) tables
            Logger?.LogInformation("Creating Pleiades database");
            StringBuilder sb = new();
            sb.AppendLine(LoadResourceText("Index.pgsql"));
            manager.CreateDatabase("pleiades",
                PleiadesDbSchema.Get(),
                sb.ToString());
            Logger?.LogInformation("Created Pleiades database");

            // seed data from binary files if present
            string sourceDir = Configuration.GetValue<string>("Data:SourceDir")!;
            if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
            {
                Logger?.LogInformation(
                    "Data source directory {Directory} not found", sourceDir);
                return;
            }

            Logger?.LogInformation("Seeding Pleiades database from {Directory}",
                sourceDir);
            string cs = string.Format(csTemplate, name);
            BulkTablesCopier copier = new(
                new PgSqlBulkTableCopier(cs));
            copier.Begin();
            copier.Read(sourceDir, CancellationToken.None,
                new Progress<string>((message) => Logger?.LogInformation(message)));
            copier.End();
            Logger?.LogInformation("Seeding completed.");
        }
    }
}
