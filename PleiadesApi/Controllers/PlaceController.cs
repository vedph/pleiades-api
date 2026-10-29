using Microsoft.AspNetCore.Mvc;
using Pleiades.Search;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace PleiadesApi.Controllers;

/// <summary>
/// Places.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlaceController"/> class.
/// </remarks>
/// <param name="search">The quick search service.</param>
[ApiController]
public sealed class PlaceController(IQuickSearch search) : ControllerBase
{
    private readonly IQuickSearch _search = search;

    /// <summary>
    /// Gets the details of the specified place, including its types, names,
    /// locations (with their GeoJSON geometries), and connections.
    /// </summary>
    /// <param name="id">The place ID (e.g. <c>579885</c> for Athens).</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Place.</returns>
    [HttpGet("api/places/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [ResponseCache(Duration = 3600)]
    public async Task<ActionResult<PlaceDetail>> GetPlace(
        [FromRoute][MaxLength(20)] string id, CancellationToken cancel)
    {
        PlaceDetail? place = await _search.GetPlaceAsync(id, cancel);
        return place == null ? NotFound() : Ok(place);
    }

    /// <summary>
    /// Gets the places nearest to the specified place, sorted by distance
    /// (nearest first), with their distance in meters.
    /// </summary>
    /// <param name="id">The reference place ID.</param>
    /// <param name="distance">The maximum distance in meters (0=any).</param>
    /// <param name="limit">The maximum count of places to get (1-100).
    /// </param>
    /// <param name="placeType">The optional place type(s), separated by
    /// commas: either full URIs or <c>/</c> followed by the last segment of
    /// a Pleiades place type URI (e.g. <c>/settlement</c>).</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Places.</returns>
    [HttpGet("api/places/{id}/nearby")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = ["*"])]
    public async Task<ActionResult<IList<QuickSearchResult>>> GetNearby(
        [FromRoute][MaxLength(20)] string id,
        [FromQuery][Range(0, 20_000_000)] int distance = 0,
        [FromQuery][Range(1, 100)] int limit = 20,
        [FromQuery][MaxLength(1000)] string? placeType = null,
        CancellationToken cancel = default)
    {
        IList<QuickSearchResult>? places = await _search.GetNearbyAsync(
            id, distance, limit, placeType, cancel);
        return places == null ? NotFound() : Ok(places);
    }
}
