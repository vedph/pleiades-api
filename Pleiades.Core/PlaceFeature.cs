namespace Pleiades.Core
{
    /// <summary>
    /// A place's geographic feature.
    /// </summary>
    public class PlaceFeature
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the geometry (GeoJSON geometry object).
        /// </summary>
        public string Geometry { get; set; }

        /// <summary>
        /// Gets or sets the snippet.
        /// </summary>
        public string Snippet { get; set; }

        /// <summary>
        /// Gets or sets the link to this feature. The URI ends with the feature
        /// ID, so we drop the original id property.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the location precision.
        /// </summary>
        public string Precision { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Type}: {Title}";
        }
    }
}
