using System.Collections.Generic;

namespace Pleiades.Core;

/// <summary>
/// An edge connecting two place nodes.
/// </summary>
public class Connection : ConnectionBase, IHasAuthors, IHasSources
{
    /// <summary>
    /// Gets or sets the connection type identifier (from connectionTypeURI).
    /// </summary>
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the certainty.
    /// </summary>
    public string? Certainty { get; set; }

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
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    public Connection()
    {
        Creators = new List<Author>();
        Contributors = new List<Author>();
        Attestations = new List<Attestation>();
        References = new List<Reference>();
    }
}
