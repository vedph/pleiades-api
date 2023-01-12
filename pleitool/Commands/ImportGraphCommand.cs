using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Pleiades.Ef;
using Pleiades.Ef.PgSql;
using Pleiades.Migration;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class ImportGraphCommand : AsyncCommand<ImportGraphCommandSettings>
{
    private static PlaceChildFlags ParseFlags(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        PlaceChildFlags flags = 0;

        foreach (char c in text.ToUpperInvariant())
        {
            switch (c)
            {
                case 'F':
                    flags |= PlaceChildFlags.Features;
                    break;
                case 'C':
                    flags |= PlaceChildFlags.Connections;
                    break;
                case 'O':
                    flags |= PlaceChildFlags.Contributors;
                    break;
                case 'L':
                    flags |= PlaceChildFlags.Locations;
                    break;
                case 'E':
                    flags |= PlaceChildFlags.Connections;
                    break;
                case 'A':
                    flags |= PlaceChildFlags.Attestations;
                    break;
                case 'R':
                    flags |= PlaceChildFlags.References;
                    break;
                case 'N':
                    flags |= PlaceChildFlags.Names;
                    break;
                case 'M':
                    flags |= PlaceChildFlags.Metadata;
                    break;
                case 'T':
                    flags |= PlaceChildFlags.TargetUris;
                    break;
                case '0':
                    flags = 0;
                    break;
            }
        }
        return flags;
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        ImportGraphCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red]IMPORT JSON DATASET[/]");
        AnsiConsole.MarkupLine($"Input file: [cyan]{settings.InputPath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        if (settings.SkipCount > 0)
            AnsiConsole.MarkupLine($"Skip: [cyan]{settings.SkipCount}[/]");
        if (settings.Limit > 0)
            AnsiConsole.MarkupLine($"Limit: [cyan]{settings.Limit}[/]");
        if (settings.IsDry)
            AnsiConsole.MarkupLine($"Dry: [cyan]{settings.IsDry}[/]");

        string csTemplate = CliAppContext.Configuration
            .GetConnectionString("Default")!;

        // create database if not exists
        if (!settings.IsDry)
        {
            AnsiConsole.Status().Start("Setup database...", ctx =>
            {
                IDbManager manager = new PgSqlDbManager(csTemplate);

                if (manager.Exists(settings.DbName))
                {
                    ctx.Status($"Clearing {settings.DbName}...");
                    ctx.Spinner(Spinner.Known.Star);
                    manager.ClearDatabase(settings.DbName);
                }
                else
                {
                    ctx.Status($"Creating {settings.DbName}...");
                    ctx.Spinner(Spinner.Known.Star);
                    manager.CreateDatabase(settings.DbName,
                        PleiadesDbSchema.Get(), null);
                }
            });
        }

        using (Stream stream = new FileStream(settings.InputPath!, FileMode.Open,
            FileAccess.Read, FileShare.Read))
        {
            JsonPlaceReader reader = new(stream, null)
            {
                Logger = CliAppContext.Logger
            };
            EfPlaceAdapter adapter = new(reader.LookupSet);

            string cs = string.Format(csTemplate, settings.DbName);
            IPleiadesContextFactory contextFactory =
                new PgSqlPleiadesContextFactory(cs);

            using EfPleiadesWriter writer = new(contextFactory)
            {
                Logger = CliAppContext.Logger
            };

            PlaceImporter importer = new(
                reader,
                writer,
                adapter)
            {
                Logger = CliAppContext.Logger,
                IsPreflight = settings.IsDry,
                Skip = settings.SkipCount,
                Limit = settings.Limit,
                ImportFlags = ParseFlags(settings.Flags)
            };

            int count = 0, oldPercent = 0;
            AnsiConsole.Progress().Start(ctx =>
            {
                var task = ctx.AddTask("Importing");
                count = importer.Import(CancellationToken.None,
                    new Progress<ProgressReport>(
                        report =>
                        {
                            task.Increment(report.Percent - oldPercent);
                            oldPercent = report.Percent;
                        }));
            });

            AnsiConsole.MarkupLine($"Places imported: [cyan]{count}[/]");
            AnsiConsole.MarkupLine($"Places read    : [cyan]{reader.Position}[/]");
        }

        return Task.FromResult(0);
    }
}

internal class ImportGraphCommandSettings : CommandSettings
{
    [CommandArgument(0, "<INPUT_PATH>")]
    public string? InputPath { get; set; }

    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    [CommandOption("-p|--preflight|--dry")]
    public bool IsDry { get; set; }

    [CommandOption("-s|--skip <VALUE>")]
    [DefaultValue(0)]
    public int SkipCount { get; set; }

    [CommandOption("-l|--limit <VALUE>")]
    [DefaultValue(0)]
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the flags to import only the specified children.
    /// </summary>
    [CommandOption("-f|--flags <COLEARNMT0>")]
    public string? Flags { get; set; }

    public ImportGraphCommandSettings()
    {
        DbName = "pleiades";
    }
}
