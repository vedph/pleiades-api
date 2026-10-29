using Pleiades.Search;
using System.ComponentModel.DataAnnotations;

namespace PleiadesApi.Models;

/// <summary>
/// Lookup data binding model. Set the page size to 0 to get all the
/// matching entries at once.
/// </summary>
/// <seealso cref="PagingBindingModel" />
public class LookupBindingModel : PagingBindingModel
{
    /// <summary>
    /// The prefix to be matched against the full name (case-insensitive),
    /// e.g. <c>https://pleiades.stoa.org/vocabularies/place-types/</c>.
    /// </summary>
    [MaxLength(200)]
    public string? Prefix { get; set; }

    /// <summary>
    /// A text to be matched against any part of the short name
    /// (case-insensitive).
    /// </summary>
    [MaxLength(100)]
    public string? ShortName { get; set; }

    /// <summary>
    /// The group to be matched.
    /// </summary>
    [MaxLength(50)]
    public string? Group { get; set; }

    /// <summary>
    /// Converts this model to a <see cref="LookupFilter"/>.
    /// </summary>
    /// <returns>Filter.</returns>
    public LookupFilter ToLookupFilter()
    {
        return new LookupFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Prefix = Prefix,
            ShortName = ShortName,
            Group = Group
        };
    }
}
