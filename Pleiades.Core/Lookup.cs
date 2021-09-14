namespace Pleiades.Core
{
    /// <summary>
    /// A lookup entry replacing a string value with a number.
    /// </summary>
    public class Lookup
    {
        /// <summary>
        /// Gets or sets the numeric identifier arbitrarily assigned to this
        /// entry. Note that this is not DB-dependent, but assigned by the
        /// dataset reader.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the optional group this lookup entry belongs to.
        /// Entries may belong to group when they refer to specific properties,
        /// i.e. the review state of an entity.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the full original name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the optional short name corresponding to
        /// <see cref="FullName"/>. Some identifiers in the original graph
        /// both include a full form (usually a URI) and a shorter, yet
        /// non-unique, name, which is registered here. Anyway, in the target
        /// database we just use the numeric <see cref="Id"/> as the identifier,
        /// which is more efficient.
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Id}: {FullName}";
        }
    }
}
