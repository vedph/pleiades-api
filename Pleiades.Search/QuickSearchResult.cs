using System.Collections.Generic;

namespace Pleiades.Search;

/// <summary>
/// The result of a quick search operation. This is a minimal set of data,
/// usually augmented by derivation.
/// </summary>
public class QuickSearchResult
{
    /// <summary>
    /// The place ID.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The URI prefix prepended to <see cref="Id"/> to get to the original
    /// resource, e.g. <c>https://pleiades.stoa.org/places/</c> for
    /// Pleiades.
    /// </summary>
    public string? UriPrefix { get; set; }

    /// <summary>
    /// The preferred place name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The first place type, if any.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// All the place types.
    /// </summary>
    public IList<string> Types { get; set; } = [];

    /// <summary>
    /// Latitude of the representative point, or null when the place
    /// is not located.
    /// </summary>
    public double? Lat { get; set; }

    /// <summary>
    /// Longitude of the representative point, or null when the place
    /// is not located.
    /// </summary>
    public double? Lng { get; set; }

    /// <summary>
    /// The distance in meters from the reference point, when a reference
    /// point was specified and the place is located.
    /// </summary>
    public double? Distance { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        return $"#{Id} {Name} ({Type}) {Lat},{Lng}";
    }
}
