namespace Pleiades.Search
{
    /// <summary>
    /// A geometry point in a search request.
    /// </summary>
    public class SearchRequestPoint
    {
        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRequestPoint"/>
        /// class.
        /// </summary>
        public SearchRequestPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRequestPoint"/>
        /// class.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        public SearchRequestPoint(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"POINT({Longitude},{Latitude})";
        }
    }
}
