using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Pleiades.Geo;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class ValidateGeoCommand : AsyncCommand<ValidateGeoCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        ValidateGeoCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green]VALIDATE GEO[/]");

        GeoValidator validator = new(
            string.Format(CliAppContext.Configuration.GetConnectionString("Default")!,
            settings.DbName),
            CliAppContext.Logger);

        int errors = 0, oldPercent = 0;
        AnsiConsole.Progress().Start(ctx =>
        {
            var task = ctx.AddTask("Validating");
            errors = validator.Validate(CancellationToken.None,
                new Progress<ProgressReport>(
                    report =>
                    {
                        task.Increment(report.Percent - oldPercent);
                        oldPercent = report.Percent;
                    }));
        });

        if (errors > 0)
        {
            AnsiConsole.Markup($"[red]Errors: {errors}[/]");
        }
        else
        {
            AnsiConsole.Write("[green]No errors.[/]");
        }

        return Task.FromResult(0);
    }
}

internal class ValidateGeoCommandSettings : CommandSettings
{
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    public ValidateGeoCommandSettings()
    {
        DbName = "pleiades";
    }
}
