namespace Pleiades.Ef;

/// <summary>
/// Place metadata.
/// </summary>
/// <seealso cref="EfMetaBase" />
public sealed class EfPlaceMeta : EfMetaBase
{
    /// <summary>
    /// Gets or sets the place identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the place.
    /// </summary>
    public EfPlace? Place { get; set; }
}
