using System;

namespace Pleiades.Core
{
    /// <summary>
    /// Base class for location.
    /// </summary>
    public abstract class LocationBase
    {
        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the start year.
        /// </summary>
        public short StartYear { get; set; }

        /// <summary>
        /// Gets or sets the end year.
        /// </summary>
        public short EndYear { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the provenance.
        /// </summary>
        public string Provenance { get; set; }

        /// <summary>
        /// Gets or sets the archaeological remains.
        /// </summary>
        public string Remains { get; set; }

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets the accuracy value.
        /// </summary>
        public double AccuracyValue { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the last modified time.
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Gets or sets the GeoJSON geometry.
        /// </summary>
        public string Geometry { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Title;
        }
    }
}
