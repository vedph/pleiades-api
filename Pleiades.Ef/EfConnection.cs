using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef;

/// <summary>
/// A connection linking two <see cref="EfPlace"/>'s.
/// </summary>
/// <seealso cref="ConnectionBase" />
public sealed class EfConnection : ConnectionBase
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the source place identifier.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the target place identifier.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the connection type identifier (from connectionTypeURI).
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Gets or sets the certainty identifier (from certainty).
    /// </summary>
    public int CertaintyId { get; set; }

    /// <summary>
    /// Gets or sets the state of the review.
    /// </summary>
    public int ReviewStateId { get; set; }

    /// <summary>
    /// Gets or sets the source place.
    /// </summary>
    public EfPlace? Source { get; set; }

    /// <summary>
    /// Gets or sets the target place.
    /// </summary>
    public EfPlace? Target { get; set; }

    /// <summary>
    /// Gets or sets the authors.
    /// </summary>
    public List<EfConnectionAuthorLink>? Authors { get; set; }

    /// <summary>
    /// Gets or sets the attestations.
    /// </summary>
    public List<EfConnectionAttestation>? Attestations { get; set; }

    /// <summary>
    /// Gets or sets the references.
    /// </summary>
    public List<EfConnectionReference>? References { get; set; }
}
