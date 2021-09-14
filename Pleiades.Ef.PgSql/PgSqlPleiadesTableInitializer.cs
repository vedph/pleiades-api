using Embix.Core;
using Embix.PgSql;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pleiades.Ef.PgSql
{
    /// <summary>
    /// PostgreSql Pleiades table initializer.
    /// </summary>
    /// <seealso cref="PgSqlTableInitializer" />
    public sealed class PgSqlPleiadesTableInitializer : PgSqlTableInitializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PgSqlPleiadesTableInitializer"/>
        /// class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public PgSqlPleiadesTableInitializer(IDbConnectionFactory factory)
            : base(factory)
        {
        }

        /// <summary>
        /// Gets the SQL.
        /// </summary>
        /// <returns>SQL.</returns>
        protected override string GetSql()
        {
            using StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Pleiades.Ef.PgSql.Assets.Schema.pgsql"),
                Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
