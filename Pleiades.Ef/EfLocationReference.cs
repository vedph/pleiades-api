using System.Collections.Generic;

namespace Pleiades.Ef
{
    /// <summary>
    /// An <see cref="EfLocation"/>'s reference.
    /// </summary>
    public sealed class EfLocationReference : EfReferenceBase
    {
        /// <summary>
        /// Gets or sets the Location identifier.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the Location.
        /// </summary>
        public EfLocation Location { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{LocationId}: " + base.ToString();
        }
    }
}
