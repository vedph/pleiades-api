using Embix.Core.Config;
using Embix.PgSql;
using Embix.Plugin.Greek;
using Pleiades.Index;
using SqlKata.Compilers;

namespace Pleiades.Ef.PgSql
{
    /// <summary>
    /// PostgreSql index builder factory for Pleiades.
    /// </summary>
    /// <seealso cref="IndexBuilderFactoryBase" />
    public sealed class PgSqlPleiadesIndexBuilderFactory : IndexBuilderFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PgSqlPleiadesIndexBuilderFactory"/>
        /// class.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="connString">The connection string.</param>
        public PgSqlPleiadesIndexBuilderFactory(string profile, string connString)
            : base(profile,
                    new PgSqlDbConnectionFactory(connString),
                    new PostgresCompiler(),
                    typeof(LanguageTextFilter).Assembly,
                    typeof(GrcRomanizerTextFilter).Assembly)
        {
        }
    }
}
