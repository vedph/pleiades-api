using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace PleiadesApi.Services;

/// <summary>
/// Configuration service. This is a singleton service providing access to
/// the application configuration, as loaded from appsettings.json,
/// appsettings.ENVNAME.json, and environment variables.
/// </summary>
public sealed class ConfigurationService(IWebHostEnvironment env)
{
    private readonly IWebHostEnvironment _env = env
        ?? throw new ArgumentNullException(nameof(env));
    private IConfiguration? _configuration;

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public IConfiguration Configuration
    {
        get
        {
            _configuration ??= new ConfigurationBuilder()
                .AddJsonFile("appsettings.json",
                    optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{_env.EnvironmentName}.json",
                    optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return _configuration;
        }
    }
}
