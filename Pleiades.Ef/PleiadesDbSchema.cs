using System.IO;
using System.Reflection;
using System.Text;

namespace Pleiades.Ef;

/// <summary>
/// Pleieades DB schema script provider.
/// </summary>
static public class PleiadesDbSchema
{
    private static string Load(string name)
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"Pleiades.Ef.Assets.{name}.pgsql")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the Pleiades DB schema, including its secondary indexes.
    /// </summary>
    /// <returns>SQL script for creating tables in database.</returns>
    public static string Get() =>
        Load("Schema") + "\n" + GetIndexes();

    /// <summary>
    /// Gets the Pleiades DB secondary indexes. This script is idempotent,
    /// so it can be used to add missing indexes to existing databases.
    /// </summary>
    /// <returns>SQL script.</returns>
    public static string GetIndexes() => Load("Indexes");
}
