using System.Collections.Generic;

namespace Pleiades.Core
{
    /// <summary>
    /// A place.
    /// </summary>
    /// <seealso cref="PlaceBase" />
    public class Place : PlaceBase, IHasAuthors, IHasSources
    {
        /// <summary>
        /// Gets or sets the type (from type). This is always "FeatureCollection".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the state of the review.
        /// </summary>
        public string ReviewState { get; set; }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        public List<PlaceFeature> Features { get; set; }

        /// <summary>
        /// Gets or sets the creators.
        /// </summary>
        public List<Author> Creators { get; set; }

        /// <summary>
        /// Gets or sets the contributors.
        /// </summary>
        public List<Author> Contributors { get; set; }

        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        public List<Location> Locations { get; set; }

        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        public List<Connection> Connections { get; set; }

        /// <summary>
        /// Gets or sets the attestations.
        /// </summary>
        public List<Attestation> Attestations { get; set; }

        /// <summary>
        /// Gets or sets the references.
        /// </summary>
        public List<Reference> References { get; set; }

        /// <summary>
        /// Gets or sets the names.
        /// </summary>
        public List<Name> Names { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public List<Metadata> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the URIs of the target places (connectsWith).
        /// </summary>
        public List<string> TargetUris { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Place"/> class.
        /// </summary>
        public Place()
        {
            Features = new List<PlaceFeature>();
            Creators = new List<Author>();
            Contributors = new List<Author>();
            Locations = new List<Location>();
            Connections = new List<Connection>();
            Attestations = new List<Attestation>();
            References = new List<Reference>();
            Names = new List<Name>();
            Metadata = new List<Metadata>();
            TargetUris = new List<string>();
        }
    }
}
