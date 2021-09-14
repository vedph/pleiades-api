namespace Pleiades.Ef
{
    /// <summary>
    /// Base class for attestation entities.
    /// </summary>
    public abstract class EfAttestationBase
    {
        /// <summary>
        /// Gets or sets the period identifier.
        /// </summary>
        public int PeriodId { get; set; }

        /// <summary>
        /// Gets or sets the confidence identifier.
        /// </summary>
        public int ConfidenceId { get; set; }

        /// <summary>
        /// Gets or sets the rank.
        /// </summary>
        public short Rank { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{PeriodId} [{ConfidenceId}]";
        }
    }
}
