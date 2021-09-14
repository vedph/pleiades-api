namespace Pleiades.Ef
{
    /// <summary>
    /// Link between <see cref="EfName"/> and <see cref="EfAuthor"/>.
    /// </summary>
    public sealed class EfNameAuthorLink
    {
        /// <summary>
        /// Gets or sets the name identifier.
        /// </summary>
        public int NameId { get; set; }

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
        public EfName Name { get; set; }

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
            return $"{NameId}-{AuthorId} [{Role}]";
        }
    }
}
