using Embix.Search;
using Embix.Search.PgSql;
using Npgsql;
using System.Data.Common;

namespace Pleiades.Search.PgSql;

/// <summary>
/// Quick search for PostgreSql with PostGIS.
/// </summary>
public sealed class PgSqlQuickSearch : QuickSearch, IQuickSearch
{
    /// <summary>
    /// Create a new instance of <see cref="PgSqlQuickSearch"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public PgSqlQuickSearch(string connectionString) : base(connectionString)
    {
    }

    /// <summary>
    /// Gets a new connection, not yet opened.
    /// </summary>
    /// <returns>The connection.</returns>
    protected override DbConnection GetConnection() =>
        new NpgsqlConnection(ConnectionString);

    /// <summary>
    /// Creates the SQL dialect.
    /// </summary>
    /// <returns>Dialect.</returns>
    protected override SqlDialect CreateDialect() => new PgSqlDialect();
}
