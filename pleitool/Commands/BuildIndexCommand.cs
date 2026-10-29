using Embix.Core;
using Embix.PgSql;
using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Pleiades.Ef.PgSql;
using Pleiades.Index;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

// this code is derived from Embix by just replacing some types
internal sealed class BuildIndexCommand : AsyncCommand<BuildIndexCommandSettings>
{
    private static string LoadText(string path)
    {
        using StreamReader reader = new(path, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public async override Task<int> ExecuteAsync(CommandContext context,
        BuildIndexCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[red underline]BUILD INDEX[/]");
        AnsiConsole.MarkupLine($"Profile path: [cyan]{settings.ProfilePath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        Serilog.Log.Information("BUILD INDEX");
        string cs = string.Format(
            CliAppContext.Configuration.GetConnectionString("Default")!,
            settings.DbName);
        PgSqlDbConnectionFactory connFactory = new(cs);

        // create the index tables if not present, or clear them
        PgSqlPleiadesTableInitializer initializer = new(connFactory);
        initializer.Initialize(settings.IsClearEnabled);

        // the metadata supplier requires the lookup data from the import
        PleiadesMetadataSupplier supplier = new(connFactory);

        // the factory owns the index writer shared by all its builders:
        // disposing it flushes and disposes the writer
        using (PgSqlPleiadesIndexBuilderFactory factory = new(
            LoadText(settings.ProfilePath!), cs))
        {
            // bulk load: building indexes once at the end is much faster
            // than updating them at each insert
            initializer.DropIndexes();

            foreach (string documentId in factory.Profile.GetDocuments()
                .Select(d => d.Id!))
            {
                using IndexBuilder builder = factory.GetBuilder(supplier,
                    CliAppContext.Logger);
                builder.PartitionCount = settings.PartitionCount;
                builder.MinPartitionSize = settings.MinPartitionSize;
                builder.RecordLimit = settings.RecordLimit;

                await AnsiConsole.Progress().StartAsync(async ctx =>
                {
                    var pt = builder.CalculatePartitionCount(documentId);
                    List<ProgressTask> tasks = new(pt.Item1);
                    for (int i = 0; i < pt.Item1; i++)
                        tasks.Add(ctx.AddTask($"{documentId} #{i + 1}"));

                    await builder.BuildAsync(documentId, cancellationToken,
                        new Progress<ProgressReport>(report =>
                        {
                            // the index builder provides messages with prefix
                            // [NNN.DocId] where NNN=partition nr.(1-N)
                            if (report.Message?.StartsWith('[') == true
                                && int.TryParse(report.Message.AsSpan(1, 3),
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture, out int p)
                                && p > 0 && p <= tasks.Count)
                            {
                                tasks[p - 1].Value = report.Percent;
                            }
                        }));
                });
            }
        }

        AnsiConsole.Status().Start("Creating indexes...",
            _ => initializer.CreateIndexes());
        AnsiConsole.MarkupLine("[green]Index completed.[/]");
        return 0;
    }
}

internal class BuildIndexCommandSettings : CommandSettings
{
    [CommandArgument(0, "<PROFILE_PATH>")]
    public string ProfilePath { get; set; }

    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    [CommandOption("-c|--clear")]
    public bool IsClearEnabled { get; set; }

    [CommandOption("-p|--partition <COUNT>")]
    [DefaultValue(2)]
    public int PartitionCount { get; set; }

    [CommandOption("-s|--size <SIZE>")]
    [DefaultValue(100)]
    public int MinPartitionSize { get; set; }

    [CommandOption("-l|--limit <COUNT>")]
    [DefaultValue(0)]
    public int RecordLimit { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildIndexCommandSettings"/>
    /// class.
    /// </summary>
    public BuildIndexCommandSettings()
    {
        ProfilePath = "";
        DbName = "pleiades";
    }
}
