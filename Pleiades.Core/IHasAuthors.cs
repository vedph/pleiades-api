using System.Collections.Generic;

namespace Pleiades.Core;

/// <summary>
/// Interface implemented by entities having creators and/or contributors.
/// </summary>
public interface IHasAuthors
{
    /// <summary>
    /// Gets or sets the creators.
    /// </summary>
    List<Author> Creators { get; set; }

    /// <summary>
    /// Gets or sets the contributors.
    /// </summary>
    List<Author> Contributors { get; set; }
}
