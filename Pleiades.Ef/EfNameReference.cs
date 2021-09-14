namespace Pleiades.Ef
{
    /// <summary>
    /// An <see cref="EfName"/>'s reference.
    /// </summary>
    public sealed class EfNameReference : EfReferenceBase
    {
        /// <summary>
        /// Gets or sets the Name identifier.
        /// </summary>
        public int NameId { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public EfName Name { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{NameId}: " + base.ToString();
        }
    }
}
