using Pleiades.Search;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlKata;
using SqlKata.Compilers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class BuildQueryCommand : AsyncCommand
{
    private readonly QuickSearchBuilder _builder;
    private readonly Compiler _compiler;
    private QuickSearchRequest _request;

    public BuildQueryCommand()
    {
        _builder = new QuickSearchBuilder();
        _compiler = new PostgresCompiler();
        _request = new QuickSearchRequest();
    }

    private static IList<double> PromptForLonLat(string message,
        string defaultValue, string resetValue = "/")
    {
        Regex nRegex = new Regex(@"(-?[0-9]+(?:\.[0-9]+))", RegexOptions.Compiled);
        string value;

        while (true)
        {
            value = AnsiConsole.Prompt(new TextPrompt<string>(message)
                .DefaultValue(defaultValue));
            if (value == resetValue) return Array.Empty<double>();

            List<Match> matches = nRegex.Matches(value).ToList();
            if (matches.Count == 2)
            {
                double lon = double.Parse(
                    value.AsSpan(matches[0].Index, matches[0].Length),
                    CultureInfo.InvariantCulture);
                double lat = double.Parse(
                    value.AsSpan(matches[1].Index, matches[1].Length),
                    CultureInfo.InvariantCulture);

                if (lon < -180 || lon > 180)
                    AnsiConsole.MarkupLine("[red]Invalid longitude[/]");
                else if (lat < -90 || lat > 90)
                    AnsiConsole.MarkupLine("[red]Invalid latitude[/]");
                else return new[] { lon, lat };
            }
            AnsiConsole.MarkupLine("[red]Invalid point[/]");
        }
    }

    private static IList<int> PromptForIntRange(string message,
        string defaultValue, string resetValue = "/")
    {
        Regex nRegex = new Regex("(-?[0-9]+)", RegexOptions.Compiled);
        string value;

        while (true)
        {
            value = AnsiConsole.Prompt(new TextPrompt<string>(message)
                .DefaultValue(defaultValue));
            if (value == resetValue) return Array.Empty<int>();

            List<Match> matches = nRegex.Matches(value).ToList();
            if (matches.Count == 2)
            {
                int min = int.Parse(
                    value.AsSpan(matches[0].Index, matches[0].Length),
                    CultureInfo.InvariantCulture);
                int max = int.Parse(
                    value.AsSpan(matches[1].Index, matches[1].Length),
                    CultureInfo.InvariantCulture);

                if (min <= max) return new[] { min, max };
            }
            AnsiConsole.MarkupLine("[red]Invalid range[/]");
        }
    }

    private void GetSpatialOptions()
    {
        AnsiConsole.MarkupLine("[underline cyan]Spatial Options[/]");

        _request.Spatial ??= new QuickSearchSpatial();

        // distance pt and min max
        string current = $"{_request.Spatial.DistancePoint?.Longitude ?? 0}," +
            $"{_request.Spatial.DistancePoint?.Longitude ?? 0}";

        IList<double> lonLat = PromptForLonLat("Reference point (lon,lat) ",
            current);
        _request.Spatial.DistancePoint = lonLat.Count == 0
            ? null : new SearchRequestPoint(lonLat[0], lonLat[1]);

        current = $"{_request.Spatial?.DistanceMin ?? 0} " +
            $"{_request.Spatial?.DistanceMax ?? 0}";
        IList<int> minMax = PromptForIntRange("Distance range (min max)", current);
        if (minMax.Count == 0)
        {
            _request.Spatial!.DistanceMin = 0;
            _request.Spatial.DistanceMax = 0;
        }
        else
        {
            _request.Spatial!.DistanceMin = minMax[0];
            _request.Spatial.DistanceMax = minMax[1];
        }

        // bbox sw ne and container
        current = $"{_request.Spatial?.BBoxSwCorner?.Longitude ?? 0} " +
            $"{_request.Spatial?.BBoxSwCorner?.Longitude ?? 0}";
        lonLat = PromptForLonLat("BBox SW (lon lat)", current);
        _request.Spatial!.BBoxSwCorner = lonLat.Count == 0
            ? null : new SearchRequestPoint(lonLat[0], lonLat[1]);

        current = $"{_request.Spatial?.BBoxNeCorner?.Longitude ?? 0} " +
            $"{_request.Spatial?.BBoxNeCorner?.Longitude ?? 0}";
        lonLat = PromptForLonLat("BBox NE (lon,lat)", current);
        _request.Spatial!.BBoxNeCorner = lonLat.Count == 0
            ? null : new SearchRequestPoint(lonLat[0], lonLat[1]);

        _request.Spatial!.IsBBoxContainer = AnsiConsole.Confirm(
            $"BBox is container? [{_request.Spatial.IsBBoxContainer}]");
    }

    private void GetOptions()
    {
        AnsiConsole.Write(new Panel(_request.ToString())
        {
            Header = new PanelHeader("options")
        });

        _request.IsMatchAnyEnabled = AnsiConsole.Confirm(
            $"Match any? ({_request.IsMatchAnyEnabled})",
            false);

        _request.PlaceType = AnsiConsole.Ask<string?>(
            $"Place type (/... or full URI: {_request.PlaceType}): ");

        string current = $"{_request.YearMin} {_request.YearMax}";
        IList<int> minMax = PromptForIntRange("Year range", current);
        if (minMax.Count == 0)
        {
            _request.YearMin = _request.YearMax = 0;
        }
        else
        {
            _request.YearMin = (short)minMax[0];
            _request.YearMax = (short)minMax[1];
        }

        current = $"{_request.RankMin} {_request.RankMax}";
        minMax = PromptForIntRange("Rank range", current);
        if (minMax.Count == 0)
        {
            _request.RankMin = _request.RankMax = 0;
        }
        else
        {
            _request.RankMin = (byte)minMax[0];
            _request.RankMax = (byte)minMax[1];
        }

        List<string> scopes = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
            .PageSize(10)
            .Title("Pick scope(s)")
            .InstructionsText("Use arrows to move and space to toggle")
            .AddChoices(new[]
            {
                "(all)",
                "plttl",
                "pldsc",
                "lcttl",
                "nmrmz",
                "nmatt",
                "nmdsc"
            }));
        _request.Scopes = scopes.Any(s => s == "all") ? null : scopes;

        if (AnsiConsole.Confirm("Set spatial options?", false))
            GetSpatialOptions();

        AnsiConsole.Write(new Panel(_request.ToString()));
    }

    public override Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.MarkupLine("[underline green]BUILD QUERY[/]");

        while (true)
        {
            try
            {
                AnsiConsole.MarkupLine(
                    "[green]Q[/]uery | " +
                    "[green]C[/]ount | " +
                    "[green]O[/]ptions | " +
                    "[yellow]R[/]eset | " +
                    "e[red]X[/]it");
                char c = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (c == 'x') break;
                if (c == 'o')
                {
                    GetOptions();
                    continue;
                }
                if (c == 'r')
                {
                    _request = new QuickSearchRequest();
                    continue;
                }
                if (c == 'q' || c == 'c')
                {
                    _request.Text = AnsiConsole.Prompt(
                        new TextPrompt<string?>("Text?")
                        .DefaultValue(_request.Text));

                    var t = _builder.Build(_request);
                    SqlResult result = _compiler.Compile(
                        c == 'c' ? t.Item2 : t.Item1);

                    AnsiConsole.MarkupLine($"[cyan]{result}[/]");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
            }
        }

        return Task.FromResult(0);
    }
}
