namespace Pleiades.Ef;

/// <summary>
/// Metadata for <see cref="EfLocation"/>.
/// </summary>
/// <seealso cref="EfMetaBase" />
public sealed class EfLocationMeta : EfMetaBase
{
    /// <summary>
    /// Gets or sets the location identifier.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// Gets or sets the location.
    /// </summary>
    public EfLocation? Location { get; set; }
}
