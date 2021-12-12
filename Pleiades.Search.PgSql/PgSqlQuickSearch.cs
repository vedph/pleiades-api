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
        /// Adapt the result to a type equal to or derived from
        /// <see cref="QuickSearchResult"/>.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>The result.</returns>
        protected override QuickSearchResult AdaptResult(dynamic result)
        {
            return new QuickSearchResult
            {
                Id = result.id,
                UriPrefix = "https://pleiades.stoa.org/places/",
                Name = result.title,
                Type = result.type,
                Lat = result.rp_lat,
                Lng = result.rp_lon,
                Payload = result
            };
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
