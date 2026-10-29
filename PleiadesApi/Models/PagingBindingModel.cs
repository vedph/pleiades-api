using System.ComponentModel.DataAnnotations;

namespace PleiadesApi.Models;

/// <summary>
/// Paging binding model.
/// </summary>
public class PagingBindingModel
{
    /// <summary>
    /// The page number (1-N). Default is 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The size of the page (max 100). Default is 20.
    /// </summary>
    [Range(0, 100)]
    public int PageSize { get; set; } = 20;
}
