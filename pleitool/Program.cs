using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Pleiades.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console;

namespace Pleiades.Cli;

public static class Program
{
#if DEBUG
    private static void DeleteLogs()
    {
        foreach (var path in Directory.EnumerateFiles(
            AppDomain.CurrentDomain.BaseDirectory, "pleiades-log*.txt"))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
#endif

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // https://github.com/serilog/serilog-sinks-file
            string logFilePath = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location) ?? "",
                    "pleiades-log.txt");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
#if DEBUG
            DeleteLogs();
#endif
            Stopwatch stopwatch = new();
            stopwatch.Start();

            CommandApp app = new();
            app.Configure(config =>
            {
                // TODO refactor as multiple level commands
                config.AddCommand<BuildIndexCommand>("index")
                    .WithDescription("Build data index in database");

                config.AddCommand<BuildQueryCommand>("query")
                    .WithDescription("Build SQL queries");

                config.AddCommand<BulkExportCommand>("export")
                    .WithDescription("Bulk export data from database into BLOB files");

                config.AddCommand<CreateDbCommand>("create-db")
                    .WithDescription("Create a new Pleiades database");

                config.AddCommand<ImportGraphCommand>("import-graph")
                    .WithDescription("Import graph data into database");

                config.AddCommand<PopulateSpatialCommand>("pop-spatial")
                    .WithDescription("Populate spatial data in an imported database");

                config.AddCommand<ScanGraphCommand>("scan-graph")
                    .WithDescription("Scan graph nodes from a Pleiades JSON file " +
                        "saving observations into the specified output directory.");

                config.AddCommand<ValidateGeoCommand>("val-geo")
                    .WithDescription("Validate geometries in Pleiades database.");
            });

            int result = await app.RunAsync(args);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine("\nTime: {0}h{1}'{2}\"",
                    stopwatch.Elapsed.Hours,
                    stopwatch.Elapsed.Minutes,
                    stopwatch.Elapsed.Seconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 2;
        }
    }
}
