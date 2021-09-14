namespace Pleiades.Ef
{
    /// <summary>
    /// An attestation for a location.
    /// </summary>
    public sealed class EfLocationAttestation : EfAttestationBase
    {
        /// <summary>
        /// Gets or sets the internal identifier (PK AI).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public EfLocation Location { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{LocationId}: {PeriodId} [{Rank}]";
        }
    }
}
