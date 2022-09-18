using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pleiades.Tool
{
    // using Microsoft.Extensions.CommandlineUtils:
    // https://gist.github.com/iamarcel/8047384bfbe9941e52817cf14a79dc34
    // console app structure:
    // https://github.com/iamarcel/dotnet-core-neat-console-starter

    internal static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                // https://github.com/serilog/serilog-sinks-file
                string logFilePath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "pleitool-log.txt");
                Log.Logger = new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Debug()
#else
                    .MinimumLevel.Information()
#endif
                    .Enrich.FromLogContext()
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Console.OutputEncoding = Encoding.Unicode;
                Stopwatch stopwatch = new();
                stopwatch.Start();

                Task.Run(async () =>
                {
                    AppOptions options = AppOptions.Parse(args);
                    if (options?.Command == null)
                    {
                        // RootCommand will have printed help
                        return 1;
                    }

                    Console.Clear();
                    await options.Command.Run();
                    return 0;
                }).Wait();

                Console.ResetColor();
                //int y = Console.CursorTop;
                //Console.CursorTop = Console.WindowTop + Console.WindowHeight - 5;
                //Console.CursorVisible = true;
                Console.WriteLine();

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine("\nTime: {0}h{1}'{2}\"",
                        stopwatch.Elapsed.Hours,
                        stopwatch.Elapsed.Minutes,
                        stopwatch.Elapsed.Seconds);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Console.CursorVisible = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                return 2;
            }
        }
    }
}
