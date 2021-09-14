using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef
{
    /// <summary>
    /// A place.
    /// </summary>
    /// <seealso cref="PlaceBase" />
    public sealed class EfPlace : PlaceBase
    {
        /// <summary>
        /// Gets or sets the state of the review.
        /// </summary>
        public int ReviewStateId { get; set; }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        public List<EfPlaceFeature> Features { get; set; }

        /// <summary>
        /// Gets or sets the creators/contributors.
        /// </summary>
        public List<EfPlaceAuthorLink> Authors { get; set; }

        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        public List<EfLocation> Locations { get; set; }

        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        public List<EfConnection> SourceConnections { get; set; }
        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        public List<EfConnection> TargetConnections { get; set; }

        /// <summary>
        /// Gets or sets the attestations.
        /// </summary>
        public List<EfPlaceAttestation> Attestations { get; set; }

        /// <summary>
        /// Gets or sets the references.
        /// </summary>
        public List<EfPlaceReference> References { get; set; }

        /// <summary>
        /// Gets or sets the names.
        /// </summary>
        public List<EfName> Names { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public List<EfPlaceMeta> Metadata { get; set; }
    }
}
