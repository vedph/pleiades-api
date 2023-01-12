namespace Pleiades.Ef;

/// <summary>
/// Direct link between two places (as expressed by connectsWith).
/// </summary>
public sealed class EfPlaceLink
{
    /// <summary>
    /// Gets or sets the source identifier.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the target identifier.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public EfPlace? Source { get; set; }

    /// <summary>
    /// Gets or sets the target.
    /// </summary>
    public EfPlace? Target { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{SourceId} => {TargetId}";
    }
}
