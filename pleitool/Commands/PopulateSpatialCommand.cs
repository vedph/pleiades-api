using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
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
        PopulateSpatialCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red]POPULATE SPATIAL[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        string csTemplate = CliAppContext.Configuration
            .GetConnectionString("Default")!;

        IDbManager manager = new PgSqlDbManager(csTemplate);
        string sql = LoadResourceText("Spatial.pgsql");
        manager.ExecuteCommands(settings.DbName, sql.Split(';'));

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
