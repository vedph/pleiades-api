using Fusi.Tools.Data;
using Pleiades.Core;

namespace Pleiades.Search;

/// <summary>
/// Quick search interface.
/// </summary>
public interface IQuickSearch
{
    /// <summary>
    /// Gets the specified page of lookup data. If you set the page size
    /// to 0, all the matching data will be retrieved at once.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>Resulting page.</returns>
    DataPage<LookupEntry> GetLookup(LookupFilter filter);

    /// <summary>
    /// Executes the search according to the specified request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>Resulting page.</returns>
    DataPage<QuickSearchResult> Execute(QuickSearchRequest request);
}
