using Microsoft.Extensions.Configuration;
using System.IO;

namespace PleiadesApi.Services
{
    /// <summary>
    /// A reader for the web application's configuration which can be used
    /// independently from web host. This is used to read settings from the
    /// app's configuration and then override them using an in-memory provider.
    /// </summary>
    public static class AppConfigReader
    {
        /// <summary>
        /// Reads the configuration root.
        /// </summary>
        /// <param name="envName">The optional environment name
        /// (e.g. <c>Development</c>).</param>
        /// <returns>Configuration.</returns>
        public static IConfiguration Read(string envName = null)
        {
            string dir = Directory.GetCurrentDirectory();

            // https://stackoverflow.com/questions/41653688/asp-net-core-appsettings-json-update-in-code
            ConfigurationBuilder cb = new();
            return string.IsNullOrEmpty(envName)
                ? cb.SetBasePath(dir)
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build()
                : cb.SetBasePath(dir)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{envName}.json")
                    .AddEnvironmentVariables()
                    .Build();
        }
    }
}
