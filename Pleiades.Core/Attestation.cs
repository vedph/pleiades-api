namespace Pleiades.Core
{
    /// <summary>
    /// An attestation.
    /// </summary>
    public class Attestation
    {
        /// <summary>
        /// Gets or sets the period identifier.
        /// </summary>
        public string PeriodId { get; set; }

        /// <summary>
        /// Gets or sets the confidence identifier.
        /// </summary>
        public string ConfidenceId { get; set; }

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
