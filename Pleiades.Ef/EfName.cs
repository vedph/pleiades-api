using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef;

/// <summary>
/// A name assigned to a place.
/// </summary>
public sealed class EfName : NameBase
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
    /// Gets or sets the type (e.g. <c>geographic</c>, <c>ethnic</c>, etc).
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Gets or sets the transcription accuracy identifier.
    /// </summary>
    public int TrAccuracyId { get; set; }

    /// <summary>
    /// Gets or sets the transcription completeness identifier.
    /// </summary>
    public int TrCompletenessId { get; set; }

    /// <summary>
    /// Gets or sets the certainty identifier.
    /// </summary>
    public int CertaintyId { get; set; }

    /// <summary>
    /// Gets or sets the state of the review.
    /// </summary>
    public int ReviewStateId { get; set; }

    /// <summary>
    /// Gets or sets the author/contributors.
    /// </summary>
    public List<EfNameAuthorLink>? Authors { get; set; }

    /// <summary>
    /// Gets or sets the attestations.
    /// </summary>
    public List<EfNameAttestation>? Attestations { get; set; }

    /// <summary>
    /// Gets or sets the references.
    /// </summary>
    public List<EfNameReference>? References { get; set; }

    /// <summary>
    /// Gets or sets the place.
    /// </summary>
    public EfPlace? Place { get; set; }
}
