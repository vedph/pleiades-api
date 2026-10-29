using Embix.Core.Filters;
using Embix.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pleiades.Search;

/// <summary>
/// Quick-search SQL code builder. This builds parameterized PostgreSQL
/// queries, requiring PostGIS for spatial filters.
/// </summary>
/// <remarks>The text tokens are matched against the <c>eix_token</c> table,
/// and each token yields the IDs of the places having a matching token
/// occurrence in the requested scopes, ranks and years; these sets are
/// intersected (all tokens must match) or united (any token must match).
/// Place-level filters (type and spatial) are then applied once to the
/// resulting places.</remarks>
public sealed partial class QuickSearchBuilder
{
    /// <summary>
    /// The URI prefix of the Pleiades place types. A place type starting
    /// with <c>/</c> is the last segment of a URI with this prefix.
    /// </summary>
    public const string PlaceTypeUriPrefix =
        "https://pleiades.stoa.org/vocabularies/place-types";

    private const string PLACE_TYPE_META = "place-type-uri";

    private readonly QueryTextClauseBuilder _clauseBuilder;
    private readonly CompositeTextFilter _filter;
    private readonly object _filterLock = new();

    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public SqlDialect Dialect { get; }

    [GeneratedRegex(@"^(?<o>=|<>|\*=|\^=|\$=|\?=|~=|%=)?(?<v>.+)")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WsRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickSearchBuilder"/>
    /// class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <exception cref="ArgumentNullException">dialect</exception>
    public QuickSearchBuilder(SqlDialect dialect)
    {
        Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _clauseBuilder = new QueryTextClauseBuilder();
        _filter = new CompositeTextFilter(
            new WhitespaceTextFilter(),
            new StandardTextFilter());
    }

    /// <summary>
    /// Gets the full URIs of the place types specified in
    /// <see cref="QuickSearchRequest.PlaceType"/>.
    /// </summary>
    /// <param name="placeType">The place type(s).</param>
    /// <returns>URIs.</returns>
    public static string[] GetPlaceTypeUris(string? placeType)
    {
        if (string.IsNullOrWhiteSpace(placeType)) return [];

        return [.. placeType.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.StartsWith('/') ? PlaceTypeUriPrefix + t : t)
            .Distinct()];
    }

    /// <summary>
    /// Parses the text tokens of the request into operator and value,
    /// filtering values as they were filtered when indexed. Tokens whose
    /// value is empty after filtering are dropped.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>Tokens.</returns>
    public IList<(string Op, string Value)> ParseText(string? text)
    {
        List<(string, string)> tokens = [];
        if (string.IsNullOrWhiteSpace(text)) return tokens;

        foreach (string token in WsRegex().Replace(text, " ").Trim().Split(' '))
        {
            Match m = TokenRegex().Match(token);
            if (!m.Success) continue;

            string op = m.Groups["o"].Length > 0 ? m.Groups["o"].Value : "=";
            string value = m.Groups["v"].Value;

            switch (op)
            {
                case "=":
                case "<>":
                case "*=":
                case "^=":
                case "$=":
                    StringBuilder sb = new(value);
                    lock (_filterLock) _filter.Apply(sb);
                    value = sb.ToString().Trim();
                    break;
                case "?=":
                case "%=":
                    value = value.ToLowerInvariant();
                    break;
            }
            if (value.Length > 0) tokens.Add((op, value));
        }
        return tokens;
    }

    private static void AddOccurrenceFilters(QuickSearchRequest request,
        SqlWhereBuilder where)
    {
        // scope
        if (request.Scopes?.Count > 0)
            where.Add($"o.field = ANY({where.AddParameter(request.Scopes.ToArray())})");

        // rank
        if (request.RankMin > 0)
            where.Add($"o.\"rank\" >= {where.AddParameter((int)request.RankMin)}");
        if (request.RankMax > 0)
            where.Add($"o.\"rank\" <= {where.AddParameter((int)request.RankMax)}");

        // years: the occurrence range must overlap the requested one
        if (request.YearMin != 0)
            where.Add($"o.year_max >= {where.AddParameter(request.YearMin)}");
        if (request.YearMax != 0)
            where.Add($"o.year_min <= {where.AddParameter(request.YearMax)}");
    }

    private static bool HasOccurrenceMetaFilters(QuickSearchRequest request) =>
        request.RankMin > 0 || request.RankMax > 0
        || request.YearMin != 0 || request.YearMax != 0;

    private string BuildTokenQuery(QuickSearchRequest request,
        SqlParameters parameters, string op, string value)
    {
        SqlWhereBuilder where = new(parameters);
        _clauseBuilder.AddClause(where, "t.value", op, value);
        AddOccurrenceFilters(request, where);

        return "SELECT o.target_id FROM eix_token t " +
            "INNER JOIN eix_occurrence o ON o.token_id=t.id " +
            where.ToSql();
    }

    private static void AddPlaceFilters(QuickSearchRequest request,
        SqlWhereBuilder where, string? point)
    {
        // type
        string[] types = GetPlaceTypeUris(request.PlaceType);
        if (types.Length > 0)
        {
            where.Add("EXISTS(SELECT 1 FROM place_meta pm " +
                "WHERE pm.place_id=p.id " +
                $"AND pm.\"name\"={where.AddParameter(PLACE_TYPE_META)} " +
                $"AND pm.value = ANY({where.AddParameter(types)}))");
        }

        QuickSearchSpatial? spatial = request.Spatial;
        if (spatial == null) return;

        // distance (meters, on the spheroid)
        if (point != null)
        {
            if (spatial.DistanceMax > 0)
            {
                where.Add($"ST_DWithin(p.geo_pt, {point}, " +
                    $"{where.AddParameter((double)spatial.DistanceMax)})");
            }
            if (spatial.DistanceMin > 0)
            {
                where.Add($"NOT ST_DWithin(p.geo_pt, {point}, " +
                    $"{where.AddParameter((double)spatial.DistanceMin)})");
            }
        }

        // bounding box (lon/lat rectangle), matched against the place's
        // bounding box
        if (spatial.BBoxSwCorner != null && spatial.BBoxNeCorner != null)
        {
            string box = "ST_MakeEnvelope(" +
                $"{where.AddParameter(spatial.BBoxSwCorner.Longitude)}, " +
                $"{where.AddParameter(spatial.BBoxSwCorner.Latitude)}, " +
                $"{where.AddParameter(spatial.BBoxNeCorner.Longitude)}, " +
                $"{where.AddParameter(spatial.BBoxNeCorner.Latitude)}, 4326)";

            where.Add(spatial.IsBBoxContainer
                ? $"ST_Covers({box}, p.geo_bbx::geometry)"
                : $"ST_Intersects(p.geo_bbx::geometry, {box})");
        }
    }

    private static string? AddPoint(QuickSearchRequest request,
        SqlParameters parameters)
    {
        SearchRequestPoint? pt = request.Spatial?.DistancePoint;
        if (pt == null) return null;

        return "ST_SetSRID(ST_MakePoint(" +
            $"{parameters.Add(pt.Longitude)}, {parameters.Add(pt.Latitude)}" +
            "), 4326)::geography";
    }

    private string? BuildMatchQuery(QuickSearchRequest request,
        SqlParameters parameters)
    {
        IList<(string Op, string Value)> tokens = ParseText(request.Text);
        if (tokens.Count == 0) return null;

        string glue = request.IsMatchAnyEnabled
            ? "\nUNION\n" : "\nINTERSECT\n";
        return string.Join(glue, tokens.Select(
            t => BuildTokenQuery(request, parameters, t.Op, t.Value)));
    }

    /// <summary>
    /// Builds the SQL expression selecting the place types array of the
    /// place aliased as <c>p</c>.
    /// </summary>
    /// <returns>SQL.</returns>
    internal static string GetTypesSql() =>
        "ARRAY(SELECT pm.value FROM place_meta pm WHERE pm.place_id=p.id " +
        $"AND pm.\"name\"='{PLACE_TYPE_META}' ORDER BY pm.id) AS types";

    /// <summary>
    /// Builds the queries from the specified request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The data and count queries. When the request page size
    /// is 0, all the matching places are returned.</returns>
    /// <exception cref="ArgumentNullException">request</exception>
    public PagedSqlQuery Build(QuickSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SqlParameters parameters = new(Dialect);
        SqlWhereBuilder where = new(parameters);
        string? point = AddPoint(request, parameters);

        // text: CTE with the matching places IDs
        string? match = BuildMatchQuery(request, parameters);
        string cte = "";
        if (match != null)
        {
            cte = $"WITH m AS (\n{match}\n)\n";
            where.Add("p.id IN (SELECT m.target_id FROM m)");
        }
        else if (HasOccurrenceMetaFilters(request))
        {
            // no text but occurrence filters: places having any occurrence
            // matching them
            SqlWhereBuilder occWhere = new(parameters);
            occWhere.Add("o.target_id=p.id");
            AddOccurrenceFilters(request, occWhere);
            where.Add("EXISTS(SELECT 1 FROM eix_occurrence o " +
                occWhere.ToSql() + ")");
        }

        AddPlaceFilters(request, where, point);
        string whereSql = where.ToSql();

        // count (with a snapshot of the parameters, as data add paging)
        SqlQuery count = new(
            $"{cte}SELECT COUNT(*) FROM place p\n{whereSql};",
            new Dictionary<string, object?>(parameters));

        // data
        StringBuilder sb = new(cte);
        sb.Append("SELECT p.id, p.title, ")
          // places without location have 0,0 coordinates
          .Append("CASE WHEN p.rp_lat=0 AND p.rp_lon=0 THEN NULL ELSE p.rp_lat END AS lat, ")
          .Append("CASE WHEN p.rp_lat=0 AND p.rp_lon=0 THEN NULL ELSE p.rp_lon END AS lon, ")
          .Append(GetTypesSql()).Append(", ")
          .Append(point != null
            ? $"ST_Distance(p.geo_pt, {point}) AS distance"
            : "NULL::double precision AS distance")
          .Append("\nFROM place p\n");
        if (whereSql.Length > 0) sb.Append(whereSql).Append('\n');

        // sort: by distance (KNN, served by the GiST index) or title
        sb.Append(request.Sort == QuickSearchSort.Distance && point != null
            ? $"ORDER BY p.geo_pt <-> {point}, p.id"
            : "ORDER BY p.title, p.id");

        if (request.PageSize > 0)
        {
            sb.Append('\n').Append(Dialect.BuildPaging(
                parameters.Add(request.PageSize),
                parameters.Add(request.GetSkipCount())));
        }
        sb.Append(';');

        return new PagedSqlQuery(new SqlQuery(sb.ToString(), parameters),
            count);
    }

    /// <summary>
    /// Builds the query for the places nearest to the specified place.
    /// </summary>
    /// <param name="placeId">The reference place ID.</param>
    /// <param name="distanceMax">The maximum distance in meters, or 0
    /// for no limit.</param>
    /// <param name="limit">The maximum count of places to return.</param>
    /// <param name="placeType">The optional place type(s), as for
    /// <see cref="QuickSearchRequest.PlaceType"/>.</param>
    /// <returns>Query.</returns>
    /// <exception cref="ArgumentNullException">placeId</exception>
    public SqlQuery BuildNearby(string placeId, int distanceMax, int limit,
        string? placeType = null)
    {
        ArgumentNullException.ThrowIfNull(placeId);

        SqlParameters parameters = new(Dialect);
        string id = parameters.Add(placeId);
        // the scalar subquery is evaluated once, so that the KNN ordering
        // can use the GiST index on geo_pt
        string point = $"(SELECT r.geo_pt FROM place r WHERE r.id={id})";

        SqlWhereBuilder where = new(parameters);
        where.Add($"p.id <> {id}");
        where.Add("p.geo_pt IS NOT NULL");
        if (distanceMax > 0)
        {
            where.Add($"ST_DWithin(p.geo_pt, {point}, " +
                $"{parameters.Add((double)distanceMax)})");
        }
        AddPlaceFilters(new QuickSearchRequest { PlaceType = placeType },
            where, null);

        string sql = "SELECT p.id, p.title, p.rp_lat AS lat, p.rp_lon AS lon, " +
            GetTypesSql() + ", " +
            $"ST_Distance(p.geo_pt, {point}) AS distance\n" +
            "FROM place p\n" +
            where.ToSql() + "\n" +
            $"ORDER BY p.geo_pt <-> {point}, p.id\n" +
            $"LIMIT {parameters.Add(limit)};";

        return new SqlQuery(sql, parameters);
    }

    /// <summary>
    /// Builds the lookup query for the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>Data and count queries.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public PagedSqlQuery BuildLookup(LookupFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        SqlParameters parameters = new(Dialect);
        SqlWhereBuilder where = new(parameters);

        if (!string.IsNullOrEmpty(filter.Group))
            where.Add($"\"group\"={where.AddParameter(filter.Group)}");

        if (!string.IsNullOrEmpty(filter.Prefix))
        {
            where.Add(Dialect.BuildLike("full_name", where.AddParameter(
                QueryTextClauseBuilder.EscapeLike(filter.Prefix) + "%"), true));
        }

        if (!string.IsNullOrEmpty(filter.ShortName))
        {
            where.Add(Dialect.BuildLike("short_name", where.AddParameter(
                "%" + QueryTextClauseBuilder.EscapeLike(filter.ShortName) + "%"),
                true));
        }

        string whereSql = where.ToSql();
        SqlQuery count = new(
            $"SELECT COUNT(*) FROM lookup {whereSql};",
            new Dictionary<string, object?>(parameters));

        StringBuilder sb = new(
            "SELECT id, full_name, short_name, \"group\" FROM lookup ");
        sb.Append(whereSql).Append(" ORDER BY \"group\", full_name, id");
        if (filter.PageSize > 0)
        {
            sb.Append(' ').Append(Dialect.BuildPaging(
                parameters.Add(filter.PageSize),
                parameters.Add(filter.GetSkipCount())));
        }
        sb.Append(';');

        return new PagedSqlQuery(new SqlQuery(sb.ToString(), parameters),
            count);
    }

    /// <summary>
    /// Formats the specified value for diagnostic purposes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String.</returns>
    public static string FormatValue(object? value) => value switch
    {
        null => "NULL",
        string s => "'" + s.Replace("'", "''") + "'",
        string[] a => "{" + string.Join(",", a) + "}",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
