using Pleiades.Core;

namespace Pleiades.Ef
{
    /// <summary>
    /// Base class for generic metadata entities.
    /// </summary>
    public abstract class EfMetaBase : Metadata
    {
        /// <summary>
        /// Gets or sets the internal identifier (PK AI).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Id}: {Name}={Value}";
        }
    }
}
