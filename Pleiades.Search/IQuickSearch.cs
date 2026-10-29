using Fusi.Tools.Data;
using Pleiades.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Resulting page.</returns>
    Task<DataPage<LookupEntry>> GetLookupAsync(LookupFilter filter,
        CancellationToken cancel = default);

    /// <summary>
    /// Executes the search according to the specified request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Resulting page.</returns>
    Task<DataPage<QuickSearchResult>> ExecuteAsync(QuickSearchRequest request,
        CancellationToken cancel = default);

    /// <summary>
    /// Gets the places nearest to the specified place, sorted by distance.
    /// </summary>
    /// <param name="placeId">The reference place ID.</param>
    /// <param name="distanceMax">The maximum distance in meters, or 0
    /// for no limit.</param>
    /// <param name="limit">The maximum count of places to get.</param>
    /// <param name="placeType">The optional place type(s), as for
    /// <see cref="QuickSearchRequest.PlaceType"/>.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Places, or null if the reference place was not found.
    /// </returns>
    Task<IList<QuickSearchResult>?> GetNearbyAsync(string placeId,
        int distanceMax, int limit, string? placeType = null,
        CancellationToken cancel = default);

    /// <summary>
    /// Gets the details of the specified place.
    /// </summary>
    /// <param name="id">The place ID.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The place, or null if not found.</returns>
    Task<PlaceDetail?> GetPlaceAsync(string id,
        CancellationToken cancel = default);
}
