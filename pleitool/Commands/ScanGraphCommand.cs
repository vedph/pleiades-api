using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Pleiades.Core;
using Pleiades.Migration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class ScanGraphCommand : ICommand
    {
        private readonly string _inputFile;
        private readonly string _outputDir;

        public ILogger Logger { get; set; }

        public ScanGraphCommand(string inputFile, string outputDir)
        {
            _inputFile = inputFile
                ?? throw new ArgumentNullException(nameof(inputFile));
            _outputDir = outputDir
                ?? throw new ArgumentNullException(nameof(outputDir));
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Scan graph nodes from a Pleiades JSON file " +
                "saving observations into the specified output directory.";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputArgument = command.Argument("[input]",
                "The input JSON file path");

            CommandArgument outputArgument = command.Argument("[output]",
                "The output directory");

            command.OnExecute(() =>
            {
                options.Command = new ScanGraphCommand(
                    inputArgument.Value,
                    outputArgument.Value)
                {
                    Logger = options.Logger
                };

                return 0;
            });
        }

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

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nSCAN GRAPH FROM JSON FILE\n");
            Console.ResetColor();

            Console.WriteLine($"Input JSON file: {_inputFile}\n" +
                              $"Output directory: {_outputDir}\n");

            if (!Directory.Exists(_outputDir)) Directory.CreateDirectory(_outputDir);

            PlaceMetrics metrics = new();

            using (Stream stream = new FileStream(_inputFile, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                JsonPlaceReader reader = new(stream, null)
                {
                    Logger = Logger
                };
                Place place;
                while ((place = reader.Read()) != null)
                {
                    metrics.Update(place);
                    Console.WriteLine(place);
                }
                Console.WriteLine("Places read: " + reader.Position);

                using (StreamWriter writer = new(
                    Path.Combine(_outputDir, "pl-report.tsv"), false,
                    Encoding.UTF8))
                {
                    WriteReport(reader, metrics, writer);
                    writer.Flush();
                }
            }

            return Task.CompletedTask;
        }
    }
}
