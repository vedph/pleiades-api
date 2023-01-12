namespace Pleiades.Ef;

/// <summary>
/// Link between <see cref="EfPlace"/> and <see cref="EfAuthor"/>.
/// </summary>
public sealed class EfPlaceAuthorLink
{
    /// <summary>
    /// Gets or sets the place identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public char Role { get; set; }

    /// <summary>
    /// Gets or sets the place.
    /// </summary>
    public EfPlace? Place { get; set; }

    /// <summary>
    /// Gets or sets the reference.
    /// </summary>
    public EfAuthor? Author { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{PlaceId}-{AuthorId} [{Role}]";
    }
}
