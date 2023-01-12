using System;

namespace Pleiades.Core;

/// <summary>
/// Base class for names.
/// </summary>
public abstract class NameBase
{
    /// <summary>
    /// Gets or sets the URI. This seems to be the combination of prefix
    /// <c>https://pleiades.stoa.org/places/</c> + place ID + <c>/</c> +
    /// local name ID (which is the id property in the original, but here
    /// is discarded as equal to the last portion of this URI).
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the language. This seems to be an ISO639 code, either
    /// 2- or 3-letters; anyway it's optional.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the start year.
    /// </summary>
    public short StartYear { get; set; }

    /// <summary>
    /// Gets or sets the end year.
    /// </summary>
    public short EndYear { get; set; }

    /// <summary>
    /// Gets or sets the attested form. This is optional and can use
    /// non-Latin characters.
    /// </summary>
    public string? Attested { get; set; }

    /// <summary>
    /// Gets or sets the romanized form, which is also the basis for the
    /// <see cref="Nid"/>. Several forms can be inserted separated by
    /// comma (e.g. <c>Dénia, Denia</c>).
    /// </summary>
    public string? Romanized { get; set; }

    /// <summary>
    /// Gets or sets the name's provenance (e.g.
    /// <c>Barrington Atlas: BAtlas 27 B3 Edeba</c>).
    /// </summary>
    public string? Provenance { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets the last modification date and time.
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
        return $"{Romanized} [{Language}]";
    }
}
