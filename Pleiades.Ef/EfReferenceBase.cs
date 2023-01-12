using Pleiades.Core;

namespace Pleiades.Ef;

/// <summary>
/// Base EF entity for references.
/// </summary>
/// <seealso cref="Reference" />
public abstract class EfReferenceBase : ReferenceBase
{
    /// <summary>
    /// Gets or sets the internal identifier (PK AI).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Gets or sets the citation type URI.
    /// </summary>
    public int CitTypeUriId { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: " + base.ToString();
    }
}
