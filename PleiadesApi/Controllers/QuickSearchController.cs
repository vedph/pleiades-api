using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pleiades.Core;
using Pleiades.Search;
using PleiadesApi.Models;

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
public class QuickSearchController(IQuickSearch search) : ControllerBase
{
    private readonly IQuickSearch _search = search;

    /// <summary>
    /// Gets the requested page of lookup data. You can set the page size
    /// to 0 to retrieve all the matching data at once.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>Page.</returns>
    [HttpGet("api/lookup")]
    [ProducesResponseType(200)]
    [Produces("application/json")]
    public ActionResult<DataPage<LookupEntry>> GetLookup(
        [FromQuery] LookupBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        return Ok(_search.GetLookup(model.ToLookupFilter()));
    }

    /// <summary>
    /// Does a quick search.
    /// </summary>
    /// <param name="model">The search model.</param>
    /// <returns>The requested page of results.</returns>
    [HttpGet("api/qsearch")]
    [ProducesResponseType(200)]
    [Produces("application/json")]
    public ActionResult<DataPage<QuickSearchResult>> Search(
        [FromQuery] QuickSearchBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        return Ok(_search.Execute(model.ToQuickSearchRequest()));
    }
}
