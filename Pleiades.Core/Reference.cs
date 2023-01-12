namespace Pleiades.Core;

/// <summary>
/// A reference, which can be attached to node, location, connection, or
/// name.
/// </summary>
public class Reference : ReferenceBase
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the citation type URI.
    /// </summary>
    public string? CitTypeUri { get; set; }
}
