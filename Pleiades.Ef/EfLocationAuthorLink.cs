namespace Pleiades.Ef
{
    /// <summary>
    /// Link between <see cref="EfLocation"/> and <see cref="EfAuthor"/>.
    /// </summary>
    public sealed class EfLocationAuthorLink
    {
        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the author identifier.
        /// </summary>
        public string AuthorId { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public char Role { get; set; }

        /// <summary>
        /// Gets or sets the place.
        /// </summary>
        public EfLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the reference.
        /// </summary>
        public EfAuthor Author { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{LocationId}-{AuthorId} [{Role}]";
        }
    }
}
