using System;

namespace Pleiades.Core
{
    /// <summary>
    /// Base class for places.
    /// The original properties subject[], placeTypes[], placeTypeURIs[],
    /// connectsWith[] are moved to metadata.
    /// </summary>
    public abstract class PlaceBase
    {
        /// <summary>
        /// Gets or sets the identifier (from id).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets the provenance.
        /// </summary>
        public string Provenance { get; set; }

        /// <summary>
        /// Gets or sets the rights.
        /// </summary>
        public string Rights { get; set; }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the last modification time.
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Gets or sets the reference point latitude.
        /// </summary>
        public double RpLat { get; set; }

        /// <summary>
        /// Gets or sets the reference point longitude.
        /// </summary>
        public double RpLon { get; set; }

        /// <summary>
        /// Gets or sets the bounding box "SW" latitude.
        /// </summary>
        public double BboxSwLat { get; set; }

        /// <summary>
        /// Gets or sets the bounding box "SW" longitude.
        /// </summary>
        public double BboxSwLon { get; set; }

        /// <summary>
        /// Gets or sets the bounding box "NE" latitude.
        /// </summary>
        public double BboxNeLat { get; set; }

        /// <summary>
        /// Gets or sets the bounding box "NE" longitude.
        /// </summary>
        public double BboxNeLon { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Id}: {Title}";
        }
    }
}
