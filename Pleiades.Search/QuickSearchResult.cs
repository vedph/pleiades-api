namespace Pleiades.Search
{
    /// <summary>
    /// The result of a quick search operation. This is a minimal set of data,
    /// usually augmented by derivation.
    /// </summary>
    public class QuickSearchResult
    {
        /// <summary>
        /// The place ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The URI prefix prepended to <see cref="Id"/> to get to the original
        /// resource, e.g. <c>https://pleiades.stoa.org/places/</c> for
        /// Pleiades.
        /// </summary>
        public string UriPrefix { get; set; }

        /// <summary>
        /// The preferred place name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The place type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Latitude.
        /// </summary>
        public double Lat { get; set; }

        /// <summary>
        /// Longitude.
        /// </summary>
        public double Lng { get; set; }

        /// <summary>
        /// A payload data object, usually representing the object returned
        /// by the original geographic data provider.
        /// </summary>
        public dynamic Payload { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return $"#{Id} {Name} ({Type}) {Lat},{Lng}";
        }
    }
}
