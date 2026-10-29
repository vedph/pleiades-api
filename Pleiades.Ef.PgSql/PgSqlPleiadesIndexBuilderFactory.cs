using Embix.Core;
using Embix.Core.Config;
using Embix.PgSql;
using Embix.Plugin.Greek;
using Pleiades.Index;

namespace Pleiades.Ef.PgSql;

/// <summary>
/// PostgreSql index builder factory for Pleiades. Its builders write via
/// <see cref="PgSqlIndexWriter"/> (bulk <c>COPY</c>), and include the
/// Pleiades and Greek filters.
/// </summary>
/// <seealso cref="IndexBuilderFactory" />
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
                typeof(LanguageTextFilter).Assembly,
                typeof(GrcRomanizerTextFilter).Assembly)
    {
    }

    /// <summary>
    /// Creates the index writer.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>Writer.</returns>
    protected override IIndexWriter CreateIndexWriter(EmbixProfile profile) =>
        new PgSqlIndexWriter(profile, ConnectionFactory);
}
