using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Data;

namespace Pleiades.Ef.PgSql;

/// <summary>
/// PostgreSql Pleiades DB context factory.
/// </summary>
/// <seealso cref="MySqlDbConnectionFactory" />
/// <seealso cref="IPleiadesContextFactory" />
public sealed class PgSqlPleiadesContextFactory : IPleiadesContextFactory
{
    private readonly string _connStr;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgSqlPleiadesContextFactory"/>
    /// class.
    /// </summary>
    /// <param name="connStr">The connection string.</param>
    public PgSqlPleiadesContextFactory(string connStr)
    {
        _connStr = connStr ?? throw new ArgumentNullException(nameof(connStr));
    }

    /// <summary>
    /// Gets a connection.
    /// </summary>
    /// <returns>Connection.</returns>
    public IDbConnection GetConnection() => new NpgsqlConnection(_connStr);

    /// <summary>
    /// Gets the DB context.
    /// </summary>
    /// <returns>Context.</returns>
    public PleiadesDbContext GetContext()
    {
        return new PleiadesDbContext(
            new DbContextOptionsBuilder<PleiadesDbContext>()
            .UseNpgsql(_connStr).Options);
    }
}
