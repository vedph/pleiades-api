using System.IO;
using System.Reflection;
using System.Text;

namespace Pleiades.Ef;

/// <summary>
/// Pleieades DB schema script provider.
/// </summary>
static public class PleiadesDbSchema
{
    /// <summary>
    /// Gets the Pleiades DB schema.
    /// </summary>
    /// <returns>SQL script for creating tables in database.</returns>
    public static string Get()
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Pleiades.Ef.Assets.Schema.pgsql")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
