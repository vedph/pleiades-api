namespace Pleiades.Core;

/// <summary>
/// A text token in the index.
/// </summary>
public class Token
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the target entity identifier.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the 5-letters field code.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the filtered value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the rank.
    /// </summary>
    public byte Rank { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the minimum language.
    /// </summary>
    public short YearMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum language.
    /// </summary>
    public short YearMax { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}
