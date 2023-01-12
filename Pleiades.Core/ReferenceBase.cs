namespace Pleiades.Core;

/// <summary>
/// Base class for references.
/// </summary>
public abstract class ReferenceBase
{
    /// <summary>
    /// Gets or sets the title (from shortTitle).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the access URI.
    /// </summary>
    public string? AccessUri { get; set; }

    /// <summary>
    /// Gets or sets the alternate URI.
    /// </summary>
    public string? AlternateUri { get; set; }

    /// <summary>
    /// Gets or sets the bibliographic URI.
    /// </summary>
    public string? BibUri { get; set; }

    /// <summary>
    /// Gets or sets the citation (from formattedCitation).
    /// </summary>
    public string? Citation { get; set; }

    /// <summary>
    /// Gets or sets the citation detail.
    /// </summary>
    public string? CitationDetail { get; set; }

    /// <summary>
    /// Gets or sets the other identifier.
    /// </summary>
    public string? OtherId { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return Title ?? base.ToString()!;
    }
}
