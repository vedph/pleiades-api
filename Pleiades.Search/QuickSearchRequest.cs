using Fusi.Tools.Data;
using System.Collections.Generic;
using System.Text;

namespace Pleiades.Search;

/// <summary>
/// A quicksearch request.
/// </summary>
public class QuickSearchRequest : PagingOptions
{
    /// <summary>
    /// The scopes (indexed fields) which can be searched: place title,
    /// description and details; location title; name romanized form,
    /// attested form, and description.
    /// </summary>
    public static readonly IReadOnlyList<string> AllScopes =
        ["plttl", "pldsc", "pldtl", "lcttl", "nmrmz", "nmatt", "nmdsc"];

    /// <summary>
    /// Gets or sets the text. This can include 1 or more tokens, separated
    /// by spaces. Each token is preceded by an optional operator; when no
    /// operator is specified, <c>=</c> (equals) is assumed. Operators are
    /// <c>=</c> (equals), <c>&lt;&gt;</c> (not equals), <c>*=</c> (contains),
    /// <c>^=</c> (starts with), <c>$=</c> (ends with), <c>?=</c> (wildcards
    /// <c>*</c> and <c>?</c>), <c>~=</c> (regular expression), <c>%=</c>
    /// (fuzzy, optionally followed by <c>:</c> and the minimum similarity
    /// threshold, 0-1, default 0.3).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to match if at least any
    /// of the tokens in <see cref="Text"/> matches (i.e. tokens in OR
    /// relation). The default is to match all the tokens (AND), i.e.
    /// this property is false.
    /// </summary>
    public bool IsMatchAnyEnabled { get; set; }

    /// <summary>
    /// Gets or sets the optional type(s) of the place to match. Each type
    /// is either a full URI, or <c>/</c> followed by the last segment of a
    /// Pleiades place type URI (e.g. <c>/settlement</c>). Multiple types
    /// are separated by commas, and are in OR relation.
    /// </summary>
    public string? PlaceType { get; set; }

    /// <summary>
    /// Gets or sets the minimum rank value. 0=do not filter by rank.
    /// </summary>
    public byte RankMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum rank value. 0=do not filter by rank.
    /// </summary>
    public byte RankMax { get; set; }

    /// <summary>
    /// Gets or sets the minimum year value (negative for BC). Matching
    /// occurrences must have a years range overlapping the requested one.
    /// 0=do not filter by minimum year.
    /// </summary>
    public short YearMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum year value (negative for BC). Matching
    /// occurrences must have a years range overlapping the requested one.
    /// 0=do not filter by maximum year.
    /// </summary>
    public short YearMax { get; set; }

    /// <summary>
    /// Gets or sets the scopes to limit the search to (see
    /// <see cref="AllScopes"/>). When null or empty, all the scopes are
    /// searched. The default scopes are <c>plttl</c>, <c>lcttl</c>,
    /// <c>nmrmz</c>.
    /// </summary>
    public List<string>? Scopes { get; set; }

    /// <summary>
    /// Gets or sets the spatial filters.
    /// </summary>
    public QuickSearchSpatial? Spatial { get; set; }

    /// <summary>
    /// Gets or sets the sort order of the results. Sorting by distance
    /// requires <see cref="QuickSearchSpatial.DistancePoint"/>.
    /// </summary>
    public QuickSearchSort Sort { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickSearchRequest"/>
    /// class.
    /// </summary>
    public QuickSearchRequest()
    {
        Scopes = ["plttl", "lcttl", "nmrmz"];
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(Text)) sb.Append("Text: ").Append(Text);

        if (IsMatchAnyEnabled)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Match any: ").Append(IsMatchAnyEnabled);
        }

        if (!string.IsNullOrEmpty(PlaceType))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Place type: ").Append(PlaceType);
        }

        if (RankMin > 0 || RankMax > 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Rank: ").Append(RankMin).Append(" - ").Append(RankMax);
        }

        if (YearMin != 0 || YearMax != 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Year: ").Append(YearMin).Append(" - ").Append(YearMax);
        }

        if (Scopes?.Count > 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Scopes: ").AppendJoin(" ", Scopes);
        }

        if (Spatial != null)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine("Spatial:").Append(Spatial);
        }

        if (Sort != QuickSearchSort.Title)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("Sort: ").Append(Sort);
        }

        return sb.ToString();
    }
}

/// <summary>
/// Sort order of quick search results.
/// </summary>
public enum QuickSearchSort
{
    /// <summary>Sort by place title.</summary>
    Title = 0,

    /// <summary>Sort by distance from the distance point (nearest first).
    /// </summary>
    Distance
}
