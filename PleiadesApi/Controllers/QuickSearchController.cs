using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pleiades.Core;
using Pleiades.Search;
using PleiadesApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PleiadesApi.Controllers;

/// <summary>
/// Quick search controller.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QuickSearchController"/>
/// class.
/// </remarks>
/// <param name="search">The quick search service.</param>
[ApiController]
public sealed class QuickSearchController(IQuickSearch search) : ControllerBase
{
    private readonly IQuickSearch _search = search;

    /// <summary>
    /// Gets the requested page of lookup data. You can set the page size
    /// to 0 to retrieve all the matching data at once. For instance, use
    /// the prefix <c>https://pleiades.stoa.org/vocabularies/place-types/</c>
    /// to get all the place types.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Page.</returns>
    [HttpGet("api/lookup")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [Produces("application/json")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = ["*"])]
    public async Task<ActionResult<DataPage<LookupEntry>>> GetLookup(
        [FromQuery] LookupBindingModel model, CancellationToken cancel)
    {
        return Ok(await _search.GetLookupAsync(model.ToLookupFilter(), cancel));
    }

    /// <summary>
    /// Searches places by text, type, rank, years, and spatial filters.
    /// Results are sorted by title, or by distance from the distance point;
    /// when this point is set, results include their distance in meters.
    /// </summary>
    /// <param name="model">The search model.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The requested page of results.</returns>
    [HttpGet("api/qsearch")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [Produces("application/json")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = ["*"])]
    public async Task<ActionResult<DataPage<QuickSearchResult>>> Search(
        [FromQuery] QuickSearchBindingModel model, CancellationToken cancel)
    {
        return Ok(await _search.ExecuteAsync(model.ToQuickSearchRequest(),
            cancel));
    }
}
