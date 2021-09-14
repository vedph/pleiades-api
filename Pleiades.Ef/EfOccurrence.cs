namespace Pleiades.Ef
{
    public sealed class EfOccurrence
    {
        /// <summary>
        /// Gets or sets the internal identifier (PK AI).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the token entity identifier.
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Gets or sets the 5-letters field code.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the rank.
        /// </summary>
        public byte Rank { get; set; }

        /// <summary>
        /// Gets or sets the minimum language.
        /// </summary>
        public short YearMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum language.
        /// </summary>
        public short YearMax { get; set; }

        /// <summary>
        /// Gets or sets the parent token.
        /// </summary>
        public EfToken Token { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Id}: [{Field}] {TokenId} ({YearMin}-{YearMax})";
        }
    }
}
