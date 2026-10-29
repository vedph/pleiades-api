using Embix.Search;
using Embix.Search.PgSql;
using System.Text.RegularExpressions;
using Xunit;

namespace Pleiades.Search.Test;

public sealed class QuickSearchBuilderTest
{
    private static readonly Regex _wsRegex = new(@"\s+");

    private static QuickSearchBuilder GetBuilder() => new(new PgSqlDialect());

    private static string Normalize(string sql) => _wsRegex.Replace(sql, " ").Trim();

    [Fact]
    public void ParseText_DefaultOperatorAndFilters_Ok()
    {
        var tokens = GetBuilder().ParseText("  Epidaurós   ^=Ath *=x.y ");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(("=", "epidauros"), tokens[0]);
        Assert.Equal(("^=", "ath"), tokens[1]);
        Assert.Equal(("*=", "xy"), tokens[2]);
    }

    [Fact]
    public void ParseText_EmptyAfterFilter_Dropped()
    {
        var tokens = GetBuilder().ParseText("- roma ...");

        Assert.Single(tokens);
        Assert.Equal(("=", "roma"), tokens[0]);
    }

    [Fact]
    public void Build_TextOnly_Ok()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            Text = "Epidaurus"
        });

        string sql = Normalize(query.Data.Sql);
        Assert.Equal(
            "WITH m AS ( SELECT o.target_id FROM eix_token t " +
            "INNER JOIN eix_occurrence o ON o.token_id=t.id " +
            "WHERE (\"t\".\"value\" = @p0) AND (o.field = ANY(@p1)) ) " +
            "SELECT p.id, p.title, " +
            "CASE WHEN p.rp_lat=0 AND p.rp_lon=0 THEN NULL ELSE p.rp_lat END AS lat, " +
            "CASE WHEN p.rp_lat=0 AND p.rp_lon=0 THEN NULL ELSE p.rp_lon END AS lon, " +
            "ARRAY(SELECT pm.value FROM place_meta pm WHERE pm.place_id=p.id " +
            "AND pm.\"name\"='place-type-uri' ORDER BY pm.id) AS types, " +
            "NULL::double precision AS distance " +
            "FROM place p WHERE p.id IN (SELECT m.target_id FROM m) " +
            "ORDER BY p.title, p.id LIMIT @p2 OFFSET @p3;",
            sql);
        Assert.Equal("epidaurus", query.Data.Parameters["p0"]);
        Assert.Equal(new[] { "plttl", "lcttl", "nmrmz" },
            query.Data.Parameters["p1"]);
        Assert.Equal(20, query.Data.Parameters["p2"]);
        Assert.Equal(0, query.Data.Parameters["p3"]);

        Assert.Equal(
            "WITH m AS ( SELECT o.target_id FROM eix_token t " +
            "INNER JOIN eix_occurrence o ON o.token_id=t.id " +
            "WHERE (\"t\".\"value\" = @p0) AND (o.field = ANY(@p1)) ) " +
            "SELECT COUNT(*) FROM place p " +
            "WHERE p.id IN (SELECT m.target_id FROM m);",
            Normalize(query.Count.Sql));
        // count has no paging parameters
        Assert.Equal(2, query.Count.Parameters.Count);
    }

    [Theory]
    [InlineData(false, "INTERSECT")]
    [InlineData(true, "UNION")]
    public void Build_TwoTokens_CombinedByMatchMode(bool any, string glue)
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            Text = "roma ^=ost",
            IsMatchAnyEnabled = any,
            Scopes = null
        });

        string sql = Normalize(query.Data.Sql);
        Assert.Contains("WHERE \"t\".\"value\" = @p0 " + glue +
            " SELECT o.target_id", sql);
        Assert.Contains("\"t\".\"value\" ILIKE @p1", sql);
        Assert.Equal("ost%", query.Data.Parameters["p1"]);
        Assert.DoesNotContain("o.field", sql);
    }

    [Fact]
    public void Build_RankAndYears_AppliedToOccurrences()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            Text = "roma",
            RankMin = 50,
            RankMax = 90,
            YearMin = -300,
            YearMax = 100,
            Scopes = null
        });

        string sql = Normalize(query.Data.Sql);
        Assert.Contains("(o.\"rank\" >= @p1) AND (o.\"rank\" <= @p2) " +
            "AND (o.year_max >= @p3) AND (o.year_min <= @p4)", sql);
        Assert.Equal((short)-300, query.Data.Parameters["p3"]);
        Assert.Equal((short)100, query.Data.Parameters["p4"]);
    }

    [Fact]
    public void Build_NoTextWithYears_UsesExists()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            YearMin = -100,
            Scopes = ["nmrmz"]
        });

        string sql = Normalize(query.Count.Sql);
        Assert.Equal("SELECT COUNT(*) FROM place p WHERE " +
            "EXISTS(SELECT 1 FROM eix_occurrence o WHERE (o.target_id=p.id) " +
            "AND (o.field = ANY(@p0)) AND (o.year_max >= @p1));", sql);
    }

    [Fact]
    public void Build_NoFilters_AllPlaces()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest());

        Assert.Equal("SELECT COUNT(*) FROM place p ;",
            Normalize(query.Count.Sql));
    }

    [Fact]
    public void Build_PlaceTypes_ExpandedAndMatchedOnce()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            PlaceType = "/settlement, https://x.org/river",
            Scopes = null
        });

        string sql = Normalize(query.Count.Sql);
        Assert.Equal("SELECT COUNT(*) FROM place p WHERE " +
            "EXISTS(SELECT 1 FROM place_meta pm WHERE pm.place_id=p.id " +
            "AND pm.\"name\"=@p0 AND pm.value = ANY(@p1));", sql);
        Assert.Equal(new[]
        {
            "https://pleiades.stoa.org/vocabularies/place-types/settlement",
            "https://x.org/river"
        }, query.Count.Parameters["p1"]);
    }

    [Fact]
    public void Build_DistanceAndSort_Ok()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            Scopes = null,
            Sort = QuickSearchSort.Distance,
            Spatial = new QuickSearchSpatial
            {
                DistancePoint = new SearchRequestPoint(23.7, 37.9),
                DistanceMin = 1000,
                DistanceMax = 50000
            }
        });

        string sql = Normalize(query.Data.Sql);
        const string pt = "ST_SetSRID(ST_MakePoint(@p0, @p1), 4326)::geography";
        Assert.Contains($"(ST_DWithin(p.geo_pt, {pt}, @p2)) AND " +
            $"(NOT ST_DWithin(p.geo_pt, {pt}, @p3))", sql);
        Assert.Contains($"ST_Distance(p.geo_pt, {pt}) AS distance", sql);
        Assert.Contains($"ORDER BY p.geo_pt <-> {pt}, p.id", sql);
        Assert.Equal(23.7, query.Data.Parameters["p0"]);
        Assert.Equal(37.9, query.Data.Parameters["p1"]);
        Assert.Equal(50000d, query.Data.Parameters["p2"]);
        Assert.Equal(1000d, query.Data.Parameters["p3"]);
    }

    [Theory]
    [InlineData(false, "ST_Intersects(p.geo_bbx::geometry, ST_MakeEnvelope(@p0, @p1, @p2, @p3, 4326))")]
    [InlineData(true, "ST_Covers(ST_MakeEnvelope(@p0, @p1, @p2, @p3, 4326), p.geo_bbx::geometry)")]
    public void Build_BBox_Ok(bool container, string expected)
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            Scopes = null,
            Spatial = new QuickSearchSpatial
            {
                BBoxSwCorner = new SearchRequestPoint(20, 35),
                BBoxNeCorner = new SearchRequestPoint(25, 40),
                IsBBoxContainer = container
            }
        });

        Assert.Equal($"SELECT COUNT(*) FROM place p WHERE {expected};",
            Normalize(query.Count.Sql));
    }

    [Fact]
    public void Build_PageSizeZero_NoPaging()
    {
        PagedSqlQuery query = GetBuilder().Build(new QuickSearchRequest
        {
            PageSize = 0
        });

        Assert.DoesNotContain("LIMIT", query.Data.Sql);
    }

    [Fact]
    public void BuildNearby_Ok()
    {
        SqlQuery query = GetBuilder().BuildNearby("579885", 10000, 5);

        string sql = Normalize(query.Sql);
        const string pt = "(SELECT r.geo_pt FROM place r WHERE r.id=@p0)";
        Assert.Contains("WHERE (p.id <> @p0) AND (p.geo_pt IS NOT NULL) " +
            $"AND (ST_DWithin(p.geo_pt, {pt}, @p1))", sql);
        Assert.Contains($"ORDER BY p.geo_pt <-> {pt}, p.id LIMIT @p2;", sql);
    }

    [Fact]
    public void BuildLookup_EscapesAndPages()
    {
        PagedSqlQuery query = GetBuilder().BuildLookup(new LookupFilter
        {
            Prefix = "https://x.org/a_b",
            ShortName = "50%",
            Group = "state",
            PageNumber = 2,
            PageSize = 10
        });

        Assert.Equal("SELECT id, full_name, short_name, \"group\" FROM lookup " +
            "WHERE (\"group\"=@p0) AND (full_name ILIKE @p1) " +
            "AND (short_name ILIKE @p2) ORDER BY \"group\", full_name, id " +
            "LIMIT @p3 OFFSET @p4;", Normalize(query.Data.Sql));
        Assert.Equal(@"https://x.org/a\_b%", query.Data.Parameters["p1"]);
        Assert.Equal(@"%50\%%", query.Data.Parameters["p2"]);
        Assert.Equal(10, query.Data.Parameters["p4"]);
    }
}
