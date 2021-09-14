using System.Collections.Generic;

namespace Pleiades.Core
{
    /// <summary>
    /// A place's location.
    /// </summary>
    public class Location : LocationBase, IHasAuthors, IHasSources
    {
        /// <summary>
        /// Gets or sets the certainty identifier (from associationCertaintyURI).
        /// </summary>
        public string CertaintyId { get; set; }

        /// <summary>
        /// Gets or sets the accuracy identifier.
        /// </summary>
        public string AccuracyId { get; set; }

        /// <summary>
        /// Gets or sets the state of the review.
        /// </summary>
        public string ReviewState { get; set; }

        /// <summary>
        /// Gets or sets the creators.
        /// </summary>
        public List<Author> Creators { get; set; }

        /// <summary>
        /// Gets or sets the contributors.
        /// </summary>
        public List<Author> Contributors { get; set; }

        /// <summary>
        /// Gets or sets the attestations.
        /// </summary>
        public List<Attestation> Attestations { get; set; }

        /// <summary>
        /// Gets or sets the references.
        /// </summary>
        public List<Reference> References { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public List<Metadata> Metadata { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> class.
        /// </summary>
        public Location()
        {
            Creators = new List<Author>();
            Contributors = new List<Author>();
            Attestations = new List<Attestation>();
            References = new List<Reference>();
            Metadata = new List<Metadata>();
        }
    }
}
