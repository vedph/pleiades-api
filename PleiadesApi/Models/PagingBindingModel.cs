using System;
using System.ComponentModel.DataAnnotations;

namespace PleiadesApi.Models
{
    /// <summary>
    /// Paging binding model.
    /// </summary>
    public class PagingBindingModel
    {
        /// <summary>
        /// The page number.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }

        /// <summary>
        /// The size of the page.
        /// </summary>
        [Range(0, 100)]
        public int PageSize { get; set; }
    }
}
