using Pleiades.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PleiadesApi.Models;

/// <summary>
/// Quick search filters.
/// </summary>
/// <seealso cref="SpatialBindingModel" />
public sealed class QuickSearchBindingModel : SpatialBindingModel
{
    /// <summary>
    /// The text to search. This can include 1 or more tokens, separated
    /// by spaces. Each token is preceded by an optional operator; when no
    /// operator is specified, <c>=</c> (equals) is assumed. Operators are
    /// <c>=</c> (equals), <c>&lt;&gt;</c> (not equals), <c>*=</c> (contains),
    /// <c>^=</c> (starts with), <c>$=</c> (ends with), <c>?=</c> (wildcards
    /// <c>*</c> and <c>?</c>), <c>~=</c> (regular expression), <c>%=</c>
    /// (fuzzy, optionally followed by <c>:</c> and the minimum similarity
    /// threshold, 0-1, default 0.3). Text is matched against tokens without
    /// diacritics and lowercase.
    /// </summary>
    [MaxLength(500)]
    public string? Text { get; set; }

    /// <summary>
    /// A value indicating whether to match if at least any of the tokens
    /// in <see cref="Text"/> matches (i.e. tokens in OR relation). The
    /// default is to match all the tokens (AND), i.e. this property is false.
    /// </summary>
    public bool MatchAny { get; set; }

    /// <summary>
    /// The optional type(s) of the place to match, separated by commas.
    /// You can specify the full URI, or just <c>/</c> followed by its last
    /// segment (e.g. <c>/settlement</c>) when using the Pleiades URI.
    /// </summary>
    [MaxLength(1000)]
    public string? PlaceType { get; set; }

    /// <summary>
    /// The minimum rank value (1-100). 0=do not filter by rank.
    /// </summary>
    [Range(0, 100)]
    public byte RankMin { get; set; }

    /// <summary>
    /// The maximum rank value (1-100). 0=do not filter by rank.
    /// </summary>
    [Range(0, 100)]
    public byte RankMax { get; set; }

    /// <summary>
    /// The minimum year value (negative for BC). Only occurrences whose
    /// years range overlaps the requested one match, so this applies to
    /// names and locations. 0=do not filter by minimum year.
    /// </summary>
    public short YearMin { get; set; }

    /// <summary>
    /// The maximum year value (negative for BC). 0=do not filter by
    /// maximum year.
    /// </summary>
    public short YearMax { get; set; }

    /// <summary>
    /// The scopes to limit the search to, comma-delimited.
    /// Possible values are <c>plttl</c> (place title), <c>pldsc</c> (place
    /// description), <c>pldtl</c> (place details), <c>lcttl</c> (location
    /// title), <c>nmrmz</c> (romanized name), <c>nmatt</c> (attested name),
    /// <c>nmdsc</c> (name description). If not specified, the search will
    /// include all the scopes.
    /// </summary>
    [MaxLength(100)]
    public string? Scopes { get; set; }

    /// <summary>
    /// The sort order: <c>title</c> (default) or <c>distance</c> (nearest
    /// first, requires the distance point).
    /// </summary>
    public QuickSearchSort Sort { get; set; }

    private List<string>? ParseScopes() => string.IsNullOrEmpty(Scopes)
        ? null
        : [.. Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries).Distinct()];

    /// <summary>
    /// Validates the consistency of the filters.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>Validation results.</returns>
    public override IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        foreach (ValidationResult result in base.Validate(validationContext))
            yield return result;

        if (PageSize < 1)
        {
            yield return new ValidationResult("Page size must be 1-100",
                [nameof(PageSize)]);
        }

        if (RankMin > 0 && RankMax > 0 && RankMin > RankMax)
        {
            yield return new ValidationResult(
                "Minimum rank is greater than maximum rank",
                [nameof(RankMin), nameof(RankMax)]);
        }

        if (YearMin != 0 && YearMax != 0 && YearMin > YearMax)
        {
            yield return new ValidationResult(
                "Minimum year is greater than maximum year",
                [nameof(YearMin), nameof(YearMax)]);
        }

        List<string>? scopes = ParseScopes();
        string[] invalid = scopes?.Except(QuickSearchRequest.AllScopes).ToArray()
            ?? [];
        if (invalid.Length > 0)
        {
            yield return new ValidationResult(
                "Invalid scope(s): " + string.Join(", ", invalid),
                [nameof(Scopes)]);
        }

        if (Sort == QuickSearchSort.Distance && !HasDistancePoint)
        {
            yield return new ValidationResult(
                "Sorting by distance requires the distance point",
                [nameof(Sort)]);
        }
    }

    /// <summary>
    /// Converts this model to <see cref="QuickSearchRequest"/>.
    /// </summary>
    /// <returns>Request.</returns>
    public QuickSearchRequest ToQuickSearchRequest()
    {
        QuickSearchRequest request = new()
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Text = Text,
            IsMatchAnyEnabled = MatchAny,
            PlaceType = PlaceType,
            RankMin = RankMin,
            RankMax = RankMax,
            YearMin = YearMin,
            YearMax = YearMax,
            Scopes = ParseScopes(),
            Sort = Sort
        };

        IList<SearchRequestPoint>? region = GetRegion();
        if (HasDistancePoint || HasBBox || region != null)
        {
            request.Spatial = new QuickSearchSpatial();

            if (HasDistancePoint)
            {
                request.Spatial.DistancePoint = new SearchRequestPoint(
                    DistanceLon!.Value, DistanceLat!.Value);
                request.Spatial.DistanceMin = DistanceMin;
                request.Spatial.DistanceMax = DistanceMax;
            }

            if (HasBBox)
            {
                request.Spatial.BBoxSwCorner = new SearchRequestPoint(
                    SwCornerLon!.Value, SwCornerLat!.Value);
                request.Spatial.BBoxNeCorner = new SearchRequestPoint(
                    NeCornerLon!.Value, NeCornerLat!.Value);
                request.Spatial.IsBBoxContainer = ContainedInBox;
            }

            if (region != null)
            {
                request.Spatial.Region = region;
                request.Spatial.IsRegionContainer = ContainedInRegion;
            }
        }

        return request;
    }
}
