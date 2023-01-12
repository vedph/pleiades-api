namespace Pleiades.Ef;

/// <summary>
/// An <see cref="EfConnection"/>'s reference.
/// </summary>
public sealed class EfConnectionReference : EfReferenceBase
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public int ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the connection.
    /// </summary>
    public EfConnection? Connection { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{ConnectionId}: " + base.ToString();
    }
}
