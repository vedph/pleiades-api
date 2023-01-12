namespace Pleiades.Ef;

/// <summary>
/// Name attestation.
/// </summary>
/// <seealso cref="EfAttestationBase" />
public sealed class EfNameAttestation : EfAttestationBase
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public int NameId { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public EfName? Name { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{NameId}: {PeriodId} [{Rank}]";
    }
}
