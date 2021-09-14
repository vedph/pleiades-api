using Microsoft.Extensions.CommandLineUtils;
using Pleiades.Search;
using Pleiades.Tool.Services;
using SqlKata;
using SqlKata.Compilers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class BuildQueryCommand : ICommand
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

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Build PostgreSql for Pleiades quick search.";
            command.HelpOption("-?|-h|--help");

            command.OnExecute(() =>
            {
                options.Command = new BuildQueryCommand();
                return 0;
            });
        }

        private static char PromptChar(string message)
        {
            ColorConsole.WriteEmbeddedColorLine(message);
            char c = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);
            Console.WriteLine();
            return c;
        }

        private static bool PromptBool(string message, bool defaultValue = false)
        {
            ColorConsole.WriteWarning(
                message + (defaultValue? " (Y/n)?" : " (y/N)?"));
            bool result = defaultValue;
            ConsoleKeyInfo info = Console.ReadKey();
            if (info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Escape)
            {
                Console.WriteLine(defaultValue ? 'y' : 'n');
                return defaultValue;
            }

            switch (char.ToLowerInvariant(info.KeyChar))
            {
                case 'y':
                    result = true;
                    break;
                case 'n':
                    result = false;
                    break;
                default:
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine(defaultValue ? 'y' : 'n');
                    break;
            }
            Console.WriteLine();

            return result;
        }

        private static string PromptString(string message, string defaultValue)
        {
            ColorConsole.WriteWarning(message);

            string s = Console.ReadLine();
            if (string.IsNullOrEmpty(s))
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(defaultValue);
                return defaultValue;
            }
            return s;
        }

        private static Tuple<int, int> PromptRange(string message,
            string defaultValue)
        {
            ColorConsole.WriteWarning(message);
            string s = Console.ReadLine();
            if (string.IsNullOrEmpty(s))
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(defaultValue);
                return null;
            }

            Match m = Regex.Match(s, @"^(-?\d+(?:\.\d+)?)?\s+-\s+(-?\d+(?:\.\d+)?)?");
            if (!m.Success) return null;

            if (!int.TryParse(m.Groups[1].Value, out int a)) a = 0;
            if (!int.TryParse(m.Groups[2].Value, out int b)) b = 0;
            return Tuple.Create(a, b);
        }

        private static Tuple<double, double> PromptPoint(string message,
            string defaultValue)
        {
            ColorConsole.WriteWarning(message);
            string s = Console.ReadLine();
            if (string.IsNullOrEmpty(s))
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(defaultValue);
                return null;
            }

            Match m = Regex.Match(s, @"^(-?\d+(?:\.\d+)?)?\s*,\s*(-?\d+(?:\.\d+)?)?");
            if (!m.Success) return null;

            if (!double.TryParse(m.Groups[1].Value, out double a)) a = 0;
            if (!double.TryParse(m.Groups[2].Value, out double b)) b = 0;
            return Tuple.Create(a, b);
        }

        private void GetSpatialOptions()
        {
            ColorConsole.WriteWrappedHeader("Spatial Options");

            if (_request.Spatial == null)
                _request.Spatial = new QuickSearchSpatial();

            // distance pt and min max
            string current = $"{_request.Spatial.DistancePoint?.Longitude ?? 0}," +
                $"{_request.Spatial.DistancePoint?.Longitude ?? 0}";
            Tuple<double, double> pt = PromptPoint("Reference point (lon,lat) " +
                $"[{current}]: ", current);
            if (pt != null)
            {
                _request.Spatial.DistancePoint =
                    new SearchRequestPoint(pt.Item1, pt.Item2);
            }

            current = $"{_request.Spatial?.DistanceMin ?? 0} - " +
                $"{_request.Spatial?.DistanceMax ?? 0}";
            Tuple<int, int> dst = PromptRange("Distance range (min - max) " +
                $"[{current}]: ", current);
            if (dst != null)
            {
                _request.Spatial.DistanceMin = dst.Item1;
                _request.Spatial.DistanceMax = dst.Item2;
            }

            // bbox sw ne and container
            current = $"{_request.Spatial?.BBoxSwCorner?.Longitude ?? 0}," +
                $"{_request.Spatial?.BBoxSwCorner?.Longitude ?? 0}";
            pt = PromptPoint($"Bbox SW (lon,lat) [{current}]: ", current);
            if (pt != null)
            {
                _request.Spatial.BBoxSwCorner =
                    new SearchRequestPoint(pt.Item1, pt.Item2);
            }

            current = $"{_request.Spatial?.BBoxNeCorner?.Longitude ?? 0}," +
                $"{_request.Spatial?.BBoxNeCorner?.Longitude ?? 0}";
            pt = PromptPoint($"Bbox NE (lon,lat) [{current}]: ", current);
            if (pt != null)
            {
                _request.Spatial.BBoxNeCorner =
                    new SearchRequestPoint(pt.Item1, pt.Item2);
            }

            _request.Spatial.IsBBoxContainer = PromptBool("BBox is container",
                _request.Spatial.IsBBoxContainer);
        }

        private void GetOptions()
        {
            ColorConsole.WriteWrappedHeader("Options");
            ColorConsole.WriteInfo(_request.ToString());
            Console.WriteLine();

            _request.IsMatchAnyEnabled = PromptBool("Match any",
                _request.IsMatchAnyEnabled);

            _request.PlaceType = PromptString("Place type (/... or full URI) [{}]: ",
                null);

            string current = $"{_request.YearMin} - {_request.YearMax}";
            var t = PromptRange($"Year range [{current}]: ", current);
            if (t != null)
            {
                _request.YearMin = (short)t.Item1;
                _request.YearMax = (short)t.Item2;
            }

            current = $"{_request.RankMin} - {_request.RankMax}";
            t = PromptRange($"Rank range [{current}]", current);
            if (t != null)
            {
                _request.RankMin = (byte)t.Item1;
                _request.RankMax = (byte)t.Item2;
            }

            current = _request.Scopes?.Count > 0 ?
                string.Join(" ", _request.Scopes) : "";
            string scopes = PromptString("Scopes (* or plttl pldsc pldtl lcttl " +
                $"nmrmz nmatt nmdsc) [{current}]: ", current);
            if (!string.IsNullOrEmpty(scopes))
            {
                if (scopes == "*")
                {
                    _request.Scopes = null;
                }
                else
                {
                    _request.Scopes = (from s in scopes.Split(
                        new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        select s).ToList();
                }
            }

            if (PromptBool("Set spatials", false)) GetSpatialOptions();

            ColorConsole.WriteInfo(_request.ToString());
            Console.WriteLine();
        }

        public Task Run()
        {
            ColorConsole.WriteWrappedHeader("BUILD QUERY",
                headerColor: ConsoleColor.Green);

            while (true)
            {
                try
                {
                    char c = PromptChar(
                        "[green]Q[/green]uery | " +
                        "[green]C[/green]ount | " +
                        "[green]O[/green]ptions | " +
                        "[yellow]R[/yellow]eset | " +
                        "e[red]X[/red]it");
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
                        _request.Text = PromptString($"Text [{_request.Text}]: ",
                            _request.Text);

                        var t = _builder.Build(_request);
                        SqlResult result = _compiler.Compile(
                            c == 'c'? t.Item2 : t.Item1);
                        ColorConsole.WriteWrappedHeader("result");
                        ColorConsole.WriteInfo(result.ToString());
                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    ColorConsole.WriteError(e.Message);
                }
            }

            return Task.CompletedTask;
        }
    }
}
