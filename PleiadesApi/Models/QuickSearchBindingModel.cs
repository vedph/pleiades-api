using Pleiades.Search;
using System;
using System.Linq;

namespace PleiadesApi.Models
{
    /// <summary>
    /// Quick search filters.
    /// </summary>
    /// <seealso cref="SpatialBindingModel" />
    public sealed class QuickSearchBindingModel : SpatialBindingModel
    {
        /// <summary>
        /// Gets or sets the text. This can include 1 or more tokens, separated
        /// by spaces. Each token is preceded by an optional operator; when no
        /// operator is specified, <c>*=</c> (contains) is assumed.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// A value indicating whether to match if at least any of the tokens
        /// in <see cref="Text"/> matches (i.e. tokens in OR relation). The
        /// default is to match all the tokens (AND), i.e. this property is false.
        /// </summary>
        public bool MatchAny { get; set; }

        /// <summary>
        /// Gets or sets the optional type of the place to match.
        /// You can specify the full URI, or just <c>/</c> followed by its last
        /// segment (e.g. <c>/settlement</c>) when using the pleiades URI.
        /// </summary>
        public string PlaceType { get; set; }

        /// <summary>
        /// The minimum rank value. 0=do not filter by rank.
        /// </summary>
        public byte RankMin { get; set; }

        /// <summary>
        /// The maximum rank value. 0=do not filter by rank.
        /// </summary>
        public byte RankMax { get; set; }

        /// <summary>
        /// The minimum year value. 0=do not filter by year.
        /// </summary>
        public short YearMin { get; set; }

        /// <summary>
        /// The maximum year value. 0=do not filter by year.
        /// </summary>
        public short YearMax { get; set; }

        /// <summary>
        /// The scopes to limit the search to, comma-delimited.
        /// Possible values are <c>plttl</c>, <c>pldsc</c>, <c>pldtl</c>,
        /// <c>lcttl</c>, <c>nmrmz</c>, <c>nmatt</c>, <c>nmdsc</c>.
        /// If not specified, the search will include all the scopes.
        /// Default scopes are <c>plttl</c>, <c>lcttl</c>, <c>nmrmz</c>.
        /// </summary>
        public string Scopes { get; set; }

        /// <summary>
        /// Converts this model to <see cref="QuickSearchRequest"/>.
        /// </summary>
        /// <returns>Request.</returns>
        public QuickSearchRequest ToQuickSearchRequest()
        {
            QuickSearchRequest request = new QuickSearchRequest
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
                Scopes = string.IsNullOrEmpty(Scopes)
                    ? null
                    : Scopes.Split(new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries).ToList(),
            };

            if ((DistanceLon != null && DistanceLat != null)
                || (NeCornerLon != null && NeCornerLat != null
                    && SwCornerLon != null && SwCornerLat != null))
            {
                request.Spatial = new QuickSearchSpatial();

                if (DistanceLon != null && (DistanceMin > 0 || DistanceMax > 0))
                {
                    request.Spatial.DistancePoint = new SearchRequestPoint(
                        DistanceLon.Value, DistanceLat.Value);
                    request.Spatial.DistanceMin = DistanceMin;
                    request.Spatial.DistanceMax = DistanceMax;
                }

                if (SwCornerLon != null)
                {
                    request.Spatial.BBoxSwCorner = new SearchRequestPoint(
                        SwCornerLon.Value, SwCornerLat.Value);
                    request.Spatial.BBoxNeCorner = new SearchRequestPoint(
                        NeCornerLon.Value, NeCornerLat.Value);
                    request.Spatial.IsBBoxContainer = ContainedInBox;
                }
            }

            return request;
        }
    }
}
