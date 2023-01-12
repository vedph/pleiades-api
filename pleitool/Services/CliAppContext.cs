using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace Pleiades.Cli.Services;

internal static class CliAppContext
{
    private static IConfiguration? _configuration;
    private static Microsoft.Extensions.Logging.ILogger? _logger;

    public static IConfiguration Configuration
    {
        get
        {
            _configuration ??= new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            return _configuration;
        }
    }

    public static Microsoft.Extensions.Logging.ILogger Logger
    {
        get
        {
            if (_logger is null)
            {
                // https://github.com/serilog/serilog-sinks-file
                string logFilePath = Path.Combine(
                    Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location) ?? "",
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

                _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger("");
            }
            return _logger;
        }
    }
}
