using Npgsql;
using System.Data;

namespace Pleiades.Search.PgSql
{
    /// <summary>
    /// Quick search for PostgreSql.
    /// </summary>
    public sealed class PgSqlQuickSearch : QuickSearch, IQuickSearch
    {
        /// <summary>
        /// Create a new instance of <see cref="PgSqlQuickSearch"/> class.
        /// </summary>
        /// <param name="connectionString"></param>
        public PgSqlQuickSearch(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        protected override IDbConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
    }
}
