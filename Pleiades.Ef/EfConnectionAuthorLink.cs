namespace Pleiades.Ef;

/// <summary>
/// Link between <see cref="EfConnection"/> and <see cref="EfAuthor"/>.
/// </summary>
public sealed class EfConnectionAuthorLink
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public int ConnectionId { get; set; }

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
    public EfConnection? Connection { get; set; }

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
        return $"{ConnectionId}-{AuthorId} [{Role}]";
    }
}
