using System.Collections.Generic;

namespace Pleiades.Core;

/// <summary>
/// A name assigned to a place.
/// </summary>
/// <seealso cref="NameBase" />
public class Name : NameBase, IHasAuthors, IHasSources
{
    /// <summary>
    /// Gets or sets the type (e.g. <c>geographic</c>, <c>ethnic</c>, etc).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the transcription accuracy. This is one from
    /// <c>accurate</c>.
    /// </summary>
    public string? TrAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the transcription completeness. This is one from
    /// <c>complete</c>.
    /// </summary>
    public string? TrCompleteness { get; set; }

    /// <summary>
    /// Gets or sets the certainty identifier (from associationCertaintyURI).
    /// </summary>
    public string? CertaintyId { get; set; }

    /// <summary>
    /// Gets or sets the state of the review.
    /// </summary>
    public string? ReviewState { get; set; }

    /// <summary>
    /// Gets or sets the creators.
    /// </summary>
    public List<Author> Creators { get; set; }

    /// <summary>
    /// Gets or sets the contributors.
    /// </summary>
    public List<Author> Contributors { get; set; }

    /// <summary>
    /// Gets or sets the attestations.
    /// </summary>
    public List<Attestation> Attestations { get; set; }

    /// <summary>
    /// Gets or sets the references.
    /// </summary>
    public List<Reference> References { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Name"/> class.
    /// </summary>
    public Name()
    {
        Creators = new List<Author>();
        Contributors = new List<Author>();
        Attestations = new List<Attestation>();
        References = new List<Reference>();
    }
}
