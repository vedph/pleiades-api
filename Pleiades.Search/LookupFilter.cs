using Fusi.Tools.Data;

namespace Pleiades.Search;

/// <summary>
/// Filter for retrieving lookup data.
/// </summary>
public class LookupFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the prefix to be matched against the fullname.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets a text to be matched against any part of the short name.
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// Gets or sets the group to be matched.
    /// </summary>
    public string? Group { get; set; }
}
