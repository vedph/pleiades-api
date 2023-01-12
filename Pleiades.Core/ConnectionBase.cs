using System;

namespace Pleiades.Core;

/// <summary>
/// Base class for an edge connecting two places.
/// </summary>
public abstract class ConnectionBase
{
    /// <summary>
    /// Gets or sets the URI.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the start year.
    /// </summary>
    public short StartYear { get; set; }

    /// <summary>
    /// Gets or sets the end year.
    /// </summary>
    public short EndYear { get; set; }

    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the provenance.
    /// </summary>
    public string? Provenance { get; set; }

    /// <summary>
    /// Gets or sets the target URI. Note that we do not import the original
    /// "id" field, which seems to be a short version of this URI, and in
    /// most cases is meaningless for humans as mostly composed of digits.
    /// </summary>
    public string? TargetUri { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets the last modification time.
    /// </summary>
    public DateTime Modified { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Uri}: {Title}";
    }
}
