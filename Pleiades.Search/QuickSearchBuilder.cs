using Embix.Core.Filters;
using Embix.Search;
using SqlKata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Pleiades.Search
{
    /// <summary>
    /// Quick-search SQL code builder.
    /// </summary>
    public class QuickSearchBuilder
    {
        private readonly Regex _tokenRegex;
        private readonly QueryTextClauseBuilder _clauseBuilder;
        private readonly CompositeTextFilter _filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickSearchBuilder"/>
        /// class.
        /// </summary>
        public QuickSearchBuilder()
        {
            _tokenRegex = new Regex(@"^(?<o>=|<>|\*=|\^=|\$=|\?=|~=|%=)?(?<v>.+)");
            _clauseBuilder = new QueryTextClauseBuilder();
            _filter = new CompositeTextFilter(
                new WhitespaceTextFilter(),
                new StandardTextFilter());
        }

        private static void AddPlaceType(QuickSearchRequest request, Query query)
        {
            // left join place_meta pm on p.id=pm.place_id
            // where pm.name='place-type-uri' and pm.value...
            if (string.IsNullOrEmpty(request.PlaceType)) return;

            query.LeftJoin("place_meta", "place.id", "place_meta.place_id");
            query.Where("place_meta.name", "place-type-uri");

            // match a value starting with / with the last part of a URI;
            // else match the full URI
            if (request.PlaceType.StartsWith("/",
                StringComparison.OrdinalIgnoreCase))
            {
                query.WhereLike("place_meta.value", "%" + request.PlaceType);
            }
            else
            {
                query.Where("place_meta.value", request.PlaceType);
            }
        }

        private static void AddSpatial(QuickSearchRequest request, Query query)
        {
            if (request.Spatial == null) return;

            QuickSearchSpatial spatial = request.Spatial;

            // distance
            if (spatial.DistancePoint != null
                && (spatial.DistanceMin > 0 || spatial.DistanceMax > 0))
            {
                if (spatial.DistanceMin > 0)
                {
                    query.WhereRaw("st_distance(" +
                        "place.geo_pt, st_setsrid(st_point(?,?), 4326)" +
                        ") >= ?",
                        spatial.DistancePoint.Longitude,
                        spatial.DistancePoint.Latitude,
                        spatial.DistanceMin);
                }

                if (spatial.DistanceMax > 0)
                {
                    query.WhereRaw("st_distance(" +
                        "place.geo_pt, st_setsrid(st_point(?,?), 4326)" +
                        ") <= ?",
                        spatial.DistancePoint.Longitude,
                        spatial.DistancePoint.Latitude,
                        spatial.DistanceMax);
                }
            }

            // bbox
            if (spatial.BBoxSwCorner != null && spatial.BBoxNeCorner != null)
            {
                double swLon = spatial.BBoxSwCorner.Longitude;
                double swLat = spatial.BBoxSwCorner.Latitude;
                double neLon = spatial.BBoxNeCorner.Longitude;
                double neLat = spatial.BBoxNeCorner.Latitude;

                query.WhereRaw(
                    "ST_Intersects(" +
                    "ST_GeomFromText(" +
                    "POLYGON((" +
                    $"{swLon} {swLat}," +
                    $"{neLon} {swLat}," +
                    $"{neLon} {neLat}," +
                    $"{swLon} {neLat}," +
                    $"{swLon} {swLat})), 4326)," +
                    "place.geo_pt)");
            }
        }

        private static Query GetNonTextQuery(QuickSearchRequest request,
            int number)
        {
            // select distinct place.id from eix_token t
            // inner join eix_occurrence o on t.id=o.token_id
            // inner join place p on o.target_id=p.id
            Query query = new Query("eix_token").As("t" + number)
                .Join("eix_occurrence", "eix_occurrence.token_id", "eix_token.id")
                .Join("place", "eix_occurrence.target_id", "place.id")
                .Select("place.id")
                .Distinct();

            // type
            AddPlaceType(request, query);

            // scope
            if (request.Scopes?.Count > 0)
                query.WhereRaw("eix_occurrence.field in (?)", request.Scopes);

            // rank
            if (request.RankMin > 0)
                query.Where("eix_occurrence.rank", ">=", (int)request.RankMin);
            if (request.RankMax > 0)
                query.Where("eix_occurrence.rank", "<=", (int)request.RankMax);

            // years
            if (request.YearMin > 0)
                query.Where("eix_occurrence.year_min", ">=", request.YearMin);
            if (request.YearMax > 0)
                query.Where("eix_occurrence.year_max", "<=", request.YearMax);

            // spatial
            AddSpatial(request, query);

            return query;
        }

        private void AddText(string token, Query query)
        {
            Match m = _tokenRegex.Match(token);
            if (!m.Success) return;     // defensive

            string op = m.Groups["o"].Length > 0 ? m.Groups["o"].Value : "=";
            string value = m.Groups["v"].Value;
            StringBuilder sb;

            switch (op)
            {
                case "=":
                case "<>":
                case "*=":
                case "^=":
                case "$=":
                    sb = new StringBuilder(value);
                    _filter.Apply(sb);
                    value = sb.ToString();
                    break;
            }
            if (value.Length > 0)
                _clauseBuilder.AddClause(query, "eix_token.value", op, value);
        }

        private static Query GetCountQuery(Query idQuery)
        {
            return new Query().From(idQuery).AsCount(new[] { "t.id" });
        }

        private static Query GetResultQuery(QuickSearchRequest request,
            Query idQuery)
        {
            return new Query()
                .From(idQuery)
                .Join("place", "place.id", "t.id")
                .LeftJoin("place_meta", "place_meta.place_id", "t.id")
                .Where("place_meta.name", "=", "place-type-uri")
                .Select("place.id", "place.title", "place.rp_lat", "place.rp_lon",
                    "place_meta.value AS type")
                .OrderBy("place.title", "place.id")
                .Offset(request.GetSkipCount())
                .Limit(request.PageSize);
        }

        /// <summary>
        /// Builds the query from the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A tuple where 1=query and 2=count query.</returns>
        /// <exception cref="ArgumentNullException">request</exception>
        public Tuple<Query,Query> Build(QuickSearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // normalize whitespace in text request, as we're going to split it
            // at each whitespace
            if (!string.IsNullOrEmpty(request.Text))
                request.Text = Regex.Replace(request.Text, @"\s+", " ").Trim();

            // create all the subqueries, one for each token
            List<Query> queries = new List<Query>();

            if (!string.IsNullOrEmpty(request.Text))
            {
                // text
                int n = 0;
                foreach (string token in request.Text.Split())
                {
                    Query tokenQuery = GetNonTextQuery(request, ++n);
                    AddText(token, tokenQuery);
                    queries.Add(tokenQuery);
                }
            }
            else
            {
                // non-text
                queries.Add(GetNonTextQuery(request, 1));
            }

            // build a concatenated query
            Query idQuery = queries[0].As("t");
            if (queries.Count > 1)
            {
                for (int i = 1; i < queries.Count; i++)
                {
                    if (request.IsMatchAnyEnabled) idQuery.Union(queries[i]);
                    else idQuery.Intersect(queries[i]);
                }
            }

            return Tuple.Create(
                GetResultQuery(request, idQuery),
                GetCountQuery(idQuery));
        }
    }
}
