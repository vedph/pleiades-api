using Fusi.Tools.Data;
using System.Collections.Generic;
using System.Text;

namespace Pleiades.Search
{
    /// <summary>
    /// A quicksearch request.
    /// </summary>
    public class QuickSearchRequest : PagingOptions
    {
        /// <summary>
        /// Gets or sets the text. This can include 1 or more tokens, separated
        /// by spaces. Each token is preceded by an optional operator; when no
        /// operator is specified, <c>*=</c> (contains) is assumed.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to match if at least any
        /// of the tokens in <see cref="Text"/> matches (i.e. tokens in OR
        /// relation). The default is to match all the tokens (AND), i.e.
        /// this property is false.
        /// </summary>
        public bool IsMatchAnyEnabled { get; set; }

        /// <summary>
        /// Gets or sets the optional type of the place to match.
        /// </summary>
        public string PlaceType { get; set; }

        /// <summary>
        /// Gets or sets the minimum rank value. 0=do not filter by rank.
        /// </summary>
        public byte RankMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum rank value. 0=do not filter by rank.
        /// </summary>
        public byte RankMax { get; set; }

        /// <summary>
        /// Gets or sets the minimum year value. 0=do not filter by year.
        /// </summary>
        public short YearMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum year value. 0=do not filter by year.
        /// </summary>
        public short YearMax { get; set; }

        /// <summary>
        /// Gets or sets the scopes to limit the search to.
        /// Possible values are <c>plttl</c>, <c>pldsc</c>, <c>pldtl</c>,
        /// <c>lcttl</c>, <c>nmrmz</c>, <c>nmatt</c>, <c>nmdsc</c>.
        /// If not specified, the search will include all the scopes.
        /// Default scopes are <c>plttl</c>, <c>lcttl</c>, <c>nmrmz</c>.
        /// </summary>
        public List<string> Scopes { get; set; }

        /// <summary>
        /// Gets or sets the spatial filters.
        /// </summary>
        public QuickSearchSpatial Spatial { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickSearchRequest"/>
        /// class.
        /// </summary>
        public QuickSearchRequest()
        {
            Scopes = new List<string> { "plttl", "lcttl", "nmrmz" };
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

            if (YearMin > 0 || YearMax > 0)
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

            return sb.ToString();
        }
    }
}
