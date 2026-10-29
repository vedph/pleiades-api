using Embix.Core;
using Embix.PgSql;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pleiades.Ef.PgSql;

/// <summary>
/// PostgreSql Pleiades index tables initializer. This replaces the Embix
/// schema and indexes with the Pleiades ones, which add metadata columns
/// (<c>rank</c>, <c>year_min</c>, <c>year_max</c>) to occurrences and
/// include them in the search indexes. Secondary indexes have the same
/// names of the Embix ones, so that <see cref="PgSqlTableInitializer.DropIndexes"/>
/// works for both.
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

    private static string LoadPleiadesScript(string name)
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Pleiades.Ef.PgSql.Assets.{name}.pgsql")!,
            Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the SQL code for the index tables definition.
    /// </summary>
    /// <returns>SQL.</returns>
    protected override string GetSql() => LoadPleiadesScript("Schema");

    /// <summary>
    /// Gets the SQL code for creating secondary indexes and constraints.
    /// </summary>
    /// <returns>SQL.</returns>
    protected override string GetIndexesSql() => LoadPleiadesScript("Indexes");
}
