namespace Pleiades.Ef;

/// <summary>
/// An attestation for a place.
/// </summary>
public sealed class EfPlaceAttestation : EfAttestationBase
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Place identifier.
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Gets or sets the Place.
    /// </summary>
    public EfPlace? Place { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{PlaceId}: {PeriodId} [{Rank}]";
    }
}
