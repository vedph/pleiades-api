using Embix.Core;
using Embix.Core.Config;
using Embix.PgSql;
using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pleiades.Cli.Services;
using Pleiades.Ef.PgSql;
using Pleiades.Index;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlKata.Compilers;
using SqlKata.Execution;
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
        BuildIndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]BUILD INDEX[/]");
        AnsiConsole.MarkupLine($"Profile path: [cyan]{settings.ProfilePath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        Serilog.Log.Information("BUILD INDEX");
        string cs = string.Format(
            CliAppContext.Configuration.GetConnectionString("Default")!,
            settings.DbName);

        IIndexBuilderFactory factory = new PgSqlPleiadesIndexBuilderFactory(
            LoadText(settings.ProfilePath!), cs);

        ITableInitializer initializer = new PgSqlPleiadesTableInitializer(
            new PgSqlDbConnectionFactory(cs));

        PleiadesMetadataSupplier supplier = new(
            new QueryFactory(new NpgsqlConnection(cs), new PostgresCompiler()));

        initializer.Initialize(settings.IsClearEnabled);

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
                {
                    tasks.Add(ctx.AddTask($"partition #{i + 1}"));
                }

                await builder.BuildAsync(documentId, CancellationToken.None,
                    new Progress<ProgressReport>(report =>
                    {
                        // the index builder provides messages with prefix
                        // [NNN.DocId] where NNN=partition nr.(1-N)
                        if (report!.Message?.StartsWith("[",
                            StringComparison.OrdinalIgnoreCase) == true)
                        {
                            int p = int.Parse(report.Message.AsSpan(1, 3),
                                CultureInfo.InvariantCulture);
                            tasks[p - 1].Value = report.Percent;
                        }
                    }));
                });
        }
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
