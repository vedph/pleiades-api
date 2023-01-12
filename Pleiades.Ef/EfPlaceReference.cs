namespace Pleiades.Ef;

/// <summary>
/// An <see cref="EfPlace"/>'s reference.
/// </summary>
public sealed class EfPlaceReference : EfReferenceBase
{
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
        return $"{PlaceId}: " + base.ToString();
    }
}
