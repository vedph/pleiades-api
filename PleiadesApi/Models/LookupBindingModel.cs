using Pleiades.Search;

namespace PleiadesApi.Models;

/// <summary>
/// Lookup data binding model.
/// </summary>
/// <seealso cref="PagingBindingModel" />
public class LookupBindingModel : PagingBindingModel
{
    /// <summary>
    /// The prefix to be matched against the fullname,
    /// e.g. <c>https://pleiades.stoa.org/vocabularies/place-types/</c>.
    /// </summary>
    public string Prefix { get; set; }

    /// <summary>
    /// A text to be matched against any part of the short name.
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// The group to be matched.
    /// </summary>
    public string Group { get; set; }

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
