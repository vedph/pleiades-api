using System.Collections.Generic;

namespace Pleiades.Core;

/// <summary>
/// Interface implemented by entities having attesetations and/or references.
/// </summary>
public interface IHasSources
{
    /// <summary>
    /// Gets or sets the attestations.
    /// </summary>
    List<Attestation> Attestations { get; set; }

    /// <summary>
    /// Gets or sets the references.
    /// </summary>
    List<Reference> References { get; set; }
}
