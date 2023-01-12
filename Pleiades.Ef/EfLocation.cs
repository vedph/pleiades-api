using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef;

/// <summary>
/// A place's location.
/// </summary>
/// <seealso cref="Location" />
public sealed class EfLocation : LocationBase
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the place identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the certainty identifier.
    /// </summary>
    public int CertaintyId { get; set; }

    /// <summary>
    /// Gets or sets the accuracy identifier.
    /// </summary>
    public int AccuracyId { get; set; }

    /// <summary>
    /// Gets or sets the review state identifier.
    /// </summary>
    public int ReviewStateId { get; set; }

    /// <summary>
    /// Gets or sets the place.
    /// </summary>
    public EfPlace? Place { get; set; }

    /// <summary>
    /// Gets or sets the creators/contributors.
    /// </summary>
    public List<EfLocationAuthorLink>? Authors { get; set; }

    /// <summary>
    /// Gets or sets the attestations.
    /// </summary>
    public List<EfLocationAttestation>? Attestations { get; set; }

    /// <summary>
    /// Gets or sets the references.
    /// </summary>
    public List<EfLocationReference>? References { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public List<EfLocationMeta>? Metadata { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: " + base.ToString();
    }
}
