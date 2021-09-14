namespace Pleiades.Ef
{
    /// <summary>
    /// Connection's attestation.
    /// </summary>
    /// <seealso cref="EfAttestationBase" />
    public sealed class EfConnectionAttestation : EfAttestationBase
    {
        /// <summary>
        /// Gets or sets the internal identifier (PK AI).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the connection identifier.
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        public EfConnection Connection { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{ConnectionId}: {PeriodId} [{Rank}]";
        }
    }
}
