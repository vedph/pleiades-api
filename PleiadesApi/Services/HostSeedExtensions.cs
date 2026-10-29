using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace PleiadesApi.Services;

/// <summary>
/// Database initializer extension to <see cref="IHost"/>.
/// See https://stackoverflow.com/questions/45148389/how-to-seed-in-entity-framework-core-2.
/// </summary>
public static class HostSeedExtensions
{
    private static Task SeedAsync(IServiceProvider serviceProvider)
    {
        AppDatabaseInitializer initializer = new(serviceProvider);
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(HostSeedExtensions));

        // retry when the database service is not yet available
        return Policy.Handle<DbException>()
            .WaitAndRetryAsync(
            [
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            ], (exception, timeSpan, _) =>
            {
                logger.LogError(exception,
                    "Unable to connect to DB (sleep {Delay}): {Message}",
                    timeSpan, exception.Message);
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

            int delay = config.GetValue<int>("SeedDelay");
            if (delay > 0)
            {
                logger.LogInformation("Waiting {Delay} seconds...", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            await SeedAsync(serviceProvider);
            return host;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database: {Message}", ex.Message);
            throw;
        }
    }
}
