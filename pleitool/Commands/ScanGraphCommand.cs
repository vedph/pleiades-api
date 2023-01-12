using Pleiades.Cli.Services;
using Pleiades.Core;
using Pleiades.Migration;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class ScanGraphCommand : AsyncCommand<ScanGraphCommandSettings>
{
    private static void WriteReport(JsonPlaceReader reader,
        PlaceMetrics metrics, TextWriter writer)
    {
        writer.WriteLine("#places\t\t");
        writer.WriteLine($"total\t{reader.Position}");

        writer.WriteLine("#lookups\t\t");
        foreach (var lookup in reader.LookupSet.GetLookups()
            .OrderBy(p => p.FullName))
        {
            writer.WriteLine($"{lookup.FullName}\t{lookup.Id}\t{lookup.ShortName}");
        }

        writer.WriteLine("#strings\t\t");
        foreach (var pair in metrics.Data.OrderBy(p => p.Key))
        {
            if (pair.Value.IsNullable)
                writer.WriteLine($"{pair.Key}:nul\t1\t");

            if (pair.Value.MinLength > -1)
                writer.WriteLine($"{pair.Key}:min\t{pair.Value.MinLength}\t");
            if (pair.Value.MaxLength > -1)
                writer.WriteLine($"{pair.Key}:max\t{pair.Value.MaxLength}\t");
        }
    }

    public override Task<int> ExecuteAsync(CommandContext context, ScanGraphCommandSettings settings)
    {
        AnsiConsole.Markup("[green]SCAN GRAPH FROM JSON FILE[/]");
        AnsiConsole.Markup($"Input JSON file: [cyan]{settings.InputPath}[/]");
        AnsiConsole.Markup($": [cyan]{settings.OutputDir}[/]");

        if (!Directory.Exists(settings.OutputDir))
            Directory.CreateDirectory(settings.OutputDir!);

        PlaceMetrics metrics = new();

        using (Stream stream = new FileStream(settings.InputPath!, FileMode.Open,
            FileAccess.Read, FileShare.Read))
        {
            JsonPlaceReader reader = new(stream, null)
            {
                Logger = CliAppContext.Logger
            };
            Place? place;
            while ((place = reader.Read()) != null)
            {
                metrics.Update(place);
                Console.WriteLine(place);
            }
            Console.WriteLine("Places read: " + reader.Position);

            using StreamWriter writer = new(
                Path.Combine(settings.OutputDir ?? "", "pl-report.tsv"), false,
                Encoding.UTF8);
            WriteReport(reader, metrics, writer);
            writer.Flush();
        }

        return Task.FromResult(0);
    }
}

internal class ScanGraphCommandSettings : CommandSettings
{
    [CommandArgument(0, "<INPUT_PATH>")]
    public string? InputPath { get; set; }
    [CommandArgument(1, "<OUTPUT_DIR>")]
    public string? OutputDir { get; set; }
}
