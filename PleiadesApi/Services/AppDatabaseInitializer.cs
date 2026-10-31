using Embix.PgSql;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager.PgSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pleiades.Ef;
using Pleiades.Ef.PgSql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PleiadesApi.Services;

/// <summary>
/// Application database initializer. When the Pleiades database does not
/// exist, this creates it with its schema (including the auth tables and
/// the text index tables). Then, when it has no places, it imports them
/// from the binary dump files in the <c>Data:SourceDir</c> directory, if
/// any. Finally, it seeds the stock users.
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

    private static object? ExecuteScalar(string cs, string sql)
    {
        using NpgsqlConnection connection = new(cs);
        connection.Open();
        using NpgsqlCommand cmd = new(sql, connection);
        return cmd.ExecuteScalar();
    }

    private static bool HasPlaceTable(string cs) =>
        ExecuteScalar(cs, "SELECT to_regclass('place') IS NOT NULL") is true;

    private void CreateDatabase(PgSqlDbManager manager, string name)
    {
        // the Pleiades schema includes the auth tables
        Logger.LogInformation("Creating database {Name}...", name);
        manager.CreateDatabase(name, PleiadesDbSchema.Get(), null);
        Logger.LogInformation("Database {Name} created", name);
    }

    private void EnsureDatabase(PgSqlDbManager manager, string name, string cs)
    {
        Logger.LogInformation("Checking for database {Name}...", name);
        if (!manager.Exists(name))
        {
            CreateDatabase(manager, name);
            return;
        }

        // a database without places table comes from a failed creation:
        // as the auth tables are created after it, it has no accounts
        if (!HasPlaceTable(cs))
        {
            Logger.LogWarning("Database {Name} exists but has no place " +
                "table (incomplete creation?): recreating it", name);
            manager.RemoveDatabase(name);
            CreateDatabase(manager, name);
            return;
        }
        Logger.LogInformation("Database {Name} exists", name);
    }

    private string? GetSourceDir()
    {
        string? dir = Configuration.GetValue<string>("Data:SourceDir");
        if (string.IsNullOrWhiteSpace(dir))
        {
            Logger.LogWarning("Data source directory not configured: " +
                "set Data:SourceDir (env DATA__SOURCEDIR) to the directory " +
                "with the binary dump files to import them");
            return null;
        }

        dir = Path.GetFullPath(dir);
        if (!Directory.Exists(dir))
        {
            Logger.LogWarning("Data source directory {Directory} not found " +
                "(Data:SourceDir): check that the dump directory is " +
                "mounted as a volume there", dir);
            return null;
        }
        return dir;
    }

    private IList<BinaryDumpFile>? GetDumpFiles(string dir)
    {
        IList<BinaryDumpFile> files = BinaryDumpImporter.GetFiles(dir);
        if (files.Count == 0)
        {
            string[] entries = [.. Directory.EnumerateFileSystemEntries(dir)
                .Take(20).Select(Path.GetFileName)!];
            Logger.LogWarning("No dump files (NNNNN_table.bin) found in " +
                "{Directory}. Its entries are: {Entries}", dir,
                entries.Length > 0 ? string.Join(", ", entries) : "(none)");
            return null;
        }

        Logger.LogInformation("Found {Count} dump files in {Directory}:",
            files.Count, dir);
        foreach (BinaryDumpFile file in files)
        {
            Logger.LogInformation("- {File}: {Length} bytes",
                Path.GetFileName(file.Path), file.Length);
        }
        return files;
    }

    private void ImportData(string name, string cs,
        PgSqlPleiadesTableInitializer indexInitializer)
    {
        long places = Convert.ToInt64(ExecuteScalar(cs,
            "SELECT COUNT(*) FROM place"));
        if (places > 0)
        {
            Logger.LogInformation("Database {Name} has {Count} places: " +
                "no data import required", name, places);
            return;
        }

        Logger.LogInformation("Database {Name} has no places: looking for " +
            "data to import", name);
        string? dir = GetSourceDir();
        if (dir == null) return;
        IList<BinaryDumpFile>? files = GetDumpFiles(dir);
        if (files == null) return;

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            // indexes are built once at the end, which is much faster
            Logger.LogInformation("Dropping text index secondary indexes...");
            indexInitializer.DropIndexes();

            bool imported = false;
            try
            {
                new BinaryDumpImporter(cs, Logger).Import(files,
                    CancellationToken.None);
                imported = true;
            }
            finally
            {
                Logger.LogInformation(
                    "Creating text index secondary indexes...");
                try
                {
                    indexInitializer.CreateIndexes();
                }
                catch (Exception ex) when (!imported)
                {
                    // do not hide the import error
                    Logger.LogError(ex, "Error creating text index " +
                        "secondary indexes: {Message}", ex.Message);
                }
            }

            Logger.LogInformation("Updating statistics...");
            ExecuteScalar(cs, "ANALYZE");

            places = Convert.ToInt64(ExecuteScalar(cs,
                "SELECT COUNT(*) FROM place"));
            Logger.LogInformation("Data import completed in {Elapsed}: " +
                "{Count} places", watch.Elapsed, places);
        }
        // transient errors are retried by the caller; any other error is
        // logged without stopping the API, and import is retried at the
        // next start, as the import transaction was rolled back
        catch (Exception ex) when (ex is not DbException { IsTransient: true })
        {
            Logger.LogError(ex, "Error importing data from {Directory} " +
                "after {Elapsed}: {Message}. The API will start without " +
                "data, and import will be retried at the next start",
                dir, watch.Elapsed, ex.Message);
        }
    }

    /// <summary>
    /// Initializes the database.
    /// </summary>
    protected override void InitDatabase()
    {
        string name = Configuration.GetValue<string>("DatabaseNames:Data")
            ?? "pleiades";
        string csTemplate = DbConnectionStrings.GetTemplate(
            Configuration, "Default");

        // no timeout, as index creation and import may take long
        string cs = new NpgsqlConnectionStringBuilder(
            DbConnectionStrings.Build(csTemplate, name))
        {
            CommandTimeout = 0
        }.ConnectionString;
        Logger.LogInformation("Data database: {Database}",
            DbConnectionStrings.Describe(cs));

        PgSqlDbManager manager = new(csTemplate);
        EnsureDatabase(manager, name, cs);

        // text index (Embix) tables (idempotent)
        Logger.LogInformation("Ensuring text index tables...");
        PgSqlPleiadesTableInitializer indexInitializer =
            new(new PgSqlDbConnectionFactory(cs));
        indexInitializer.Initialize(false);

        ImportData(name, cs, indexInitializer);
    }
}
