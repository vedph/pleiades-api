using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Pleiades.Ef;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class PopulateSpatialCommand :
    AsyncCommand<PopulateSpatialCommandSettings>
{
    private static string LoadResourceText(string name)
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"Pleiades.Cli.Assets.{name}")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        PopulateSpatialCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[underline red]POPULATE SPATIAL[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        string csTemplate = CliAppContext.Configuration
            .GetConnectionString("Default")!;

        IDbManager manager = new PgSqlDbManager(csTemplate);

        AnsiConsole.Status().Start("Populating spatial data...", ctx =>
        {
            manager.ExecuteCommands(settings.DbName,
                LoadResourceText("Spatial.pgsql").Split(';'));

            // ensure that all the indexes are present (this also upgrades
            // databases created by previous versions)
            ctx.Status("Creating indexes...");
            manager.ExecuteCommands(settings.DbName,
                PleiadesDbSchema.GetIndexes().Split(';'));

            ctx.Status("Analyzing...");
            manager.ExecuteCommands(settings.DbName, "ANALYZE;");
        });
        AnsiConsole.MarkupLine("[green]Completed.[/]");

        return Task.FromResult(0);
    }
}

internal class PopulateSpatialCommandSettings : CommandSettings
{
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    public PopulateSpatialCommandSettings()
    {
        DbName = "pleiades";
    }
}
