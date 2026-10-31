using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Globalization;

namespace PleiadesApi.Services;

/// <summary>
/// Helper for the PostgreSQL connection strings in configuration. These are
/// templates where the database name is a <c>{0}</c> placeholder, e.g.
/// <c>Host=localhost;...;Database={0}</c>.
/// </summary>
public static class DbConnectionStrings
{
    /// <summary>
    /// The database name placeholder in connection string templates.
    /// </summary>
    public const string DB_PLACEHOLDER = "{0}";

    /// <summary>
    /// Gets the connection string template with the specified name from
    /// <c>ConnectionStrings</c>, falling back to <c>Default</c> when it is
    /// not set. When the connection string has no <c>{0}</c> placeholder
    /// (e.g. <c>...;Database=pleiades;</c>), its database is replaced with
    /// the placeholder, so that the template can be used both to connect to
    /// the server's maintenance database and to the target database.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="name">The connection string name.</param>
    /// <returns>Template.</returns>
    /// <exception cref="InvalidOperationException">No connection string.
    /// </exception>
    public static string GetTemplate(IConfiguration config, string name)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(name);

        string? cs = config.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(cs) && name != "Default")
        {
            Serilog.Log.Information(
                "Connection string {Name} not set, using Default", name);
            cs = config.GetConnectionString("Default");
        }
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                $"Connection string \"{name}\" not configured " +
                $"(ConnectionStrings:{name})");
        }

        if (cs.Contains(DB_PLACEHOLDER, StringComparison.Ordinal)) return cs;

        NpgsqlConnectionStringBuilder builder = new(cs)
        {
            Database = DB_PLACEHOLDER
        };
        Serilog.Log.Warning("Connection string {Name} has no {Placeholder} " +
            "placeholder for the database name: using {Template}",
            name, DB_PLACEHOLDER, Describe(builder.ConnectionString));
        return builder.ConnectionString;
    }

    /// <summary>
    /// Builds the connection string for the specified database.
    /// </summary>
    /// <param name="template">The connection string template.</param>
    /// <param name="database">The database name.</param>
    /// <returns>Connection string.</returns>
    public static string Build(string template, string database) =>
        string.Format(CultureInfo.InvariantCulture, template, database);

    /// <summary>
    /// Describes the specified connection string for logging, i.e. without
    /// its password.
    /// </summary>
    /// <param name="cs">The connection string (or template).</param>
    /// <returns>Description.</returns>
    public static string Describe(string cs)
    {
        try
        {
            NpgsqlConnectionStringBuilder builder = new(cs);
            return $"Host={builder.Host};Port={builder.Port};" +
                $"Database={builder.Database};Username={builder.Username}";
        }
        catch (ArgumentException)
        {
            return "(invalid connection string)";
        }
    }
}
