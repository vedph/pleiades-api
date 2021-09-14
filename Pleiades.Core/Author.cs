namespace Pleiades.Core
{
    /// <summary>
    /// Author or contributor.
    /// </summary>
    public class Author
    {
        /// <summary>
        /// Gets or sets the author's identifier. This corresponds to Pleiades
        /// username. Note that the URI property is not imported as it can be
        /// calculated from the username.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the homepage.
        /// </summary>
        public string Homepage { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Id}: {Name}";
        }
    }
}
