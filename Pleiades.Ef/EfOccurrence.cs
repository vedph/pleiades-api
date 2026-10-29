namespace Pleiades.Ef;

/// <summary>
/// A token's occurrence in the text index.
/// </summary>
public sealed class EfOccurrence
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the token identifier.
    /// </summary>
    public int TokenId { get; set; }

    /// <summary>
    /// Gets or sets the 5-letters field code.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the target place identifier.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the rank.
    /// </summary>
    public int? Rank { get; set; }

    /// <summary>
    /// Gets or sets the minimum year.
    /// </summary>
    public short? YearMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum year.
    /// </summary>
    public short? YearMax { get; set; }

    /// <summary>
    /// Gets or sets the parent token.
    /// </summary>
    public EfToken? Token { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: [{Field}] {TokenId}@{TargetId} ({YearMin}-{YearMax})";
    }
}
