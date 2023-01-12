using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef;

/// <summary>
/// A geographic feature of a place.
/// </summary>
public sealed class EfPlaceFeature : PlaceFeature
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the place identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the place.
    /// </summary>
    public EfPlace? Place { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: " + base.ToString();
    }
}
