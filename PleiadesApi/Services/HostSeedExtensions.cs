using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PleiadesApi.Services;

/// <summary>
/// Database initializer extension to <see cref="IHost"/>.
/// See https://stackoverflow.com/questions/45148389/how-to-seed-in-entity-framework-core-2.
/// </summary>
public static class HostSeedExtensions
{
    // delays between attempts when the database service is not available
    private static readonly TimeSpan[] _retryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(60)
    ];

    private static Task SeedAsync(IServiceProvider serviceProvider,
        ILogger logger)
    {
        AppDatabaseInitializer initializer = new(serviceProvider);

        // retry only transient errors, like the database service not yet
        // accepting connections; other errors (e.g. wrong credentials)
        // would just fail again
        return Policy.Handle<DbException>(ex => ex.IsTransient)
            .WaitAndRetryAsync(_retryDelays, (exception, delay, attempt, _) =>
            {
                logger.LogWarning(exception,
                    "Database not available (attempt {Attempt}/{Count}), " +
                    "retrying in {Delay}: {Message}",
                    attempt, _retryDelays.Length + 1, delay, exception.Message);
            })
            .ExecuteAsync(initializer.SeedAsync);
    }

    /// <summary>
    /// Seeds the database.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The received host, to allow concatenation.</returns>
    public static async Task<IHost> SeedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(HostSeedExtensions));

        try
        {
            IConfiguration config =
                serviceProvider.GetRequiredService<IConfiguration>();

            logger.LogInformation("Database seeding configuration: " +
                "data database={DataDb}, auth database={AuthDb}, " +
                "source directory={SourceDir}, delay={Delay}s",
                config.GetValue<string>("DatabaseNames:Data"),
                config.GetValue<string>("DatabaseNames:Auth"),
                config.GetValue<string>("Data:SourceDir"),
                config.GetValue<int>("SeedDelay"));

            int delay = config.GetValue<int>("SeedDelay");
            if (delay > 0)
            {
                logger.LogInformation("Waiting {Delay} seconds before " +
                    "seeding...", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            logger.LogInformation("Seeding database...");
            Stopwatch watch = Stopwatch.StartNew();
            await SeedAsync(serviceProvider, logger);
            logger.LogInformation("Database seeding completed in {Elapsed}",
                watch.Elapsed);
            return host;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database: {Message}", ex.Message);
            throw;
        }
    }
}
