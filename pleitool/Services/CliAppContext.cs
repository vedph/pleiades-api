using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
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

    /// <summary>
    /// Configures the global Serilog logger, writing to <c>pleitool-log*.txt</c>
    /// in the tool's directory. The minimum level is read from the
    /// <c>LogLevel</c> setting (default <c>Information</c>). Note that at
    /// <c>Debug</c> level the indexer logs each token, producing huge logs.
    /// </summary>
    public static void ConfigureLogger()
    {
        // https://github.com/serilog/serilog-sinks-file
        string logFilePath = Path.Combine(
            Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location) ?? "",
                "pleitool-log.txt");

        if (!Enum.TryParse(Configuration["LogLevel"], true,
            out LogEventLevel level))
        {
            level = LogEventLevel.Information;
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _logger = null;
    }

    public static Microsoft.Extensions.Logging.ILogger Logger
    {
        get
        {
            _logger ??= new SerilogLoggerFactory(Log.Logger).CreateLogger("");
            return _logger;
        }
    }
}
