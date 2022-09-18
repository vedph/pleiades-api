using SqlKata;
using SqlKata.Compilers;
using System.Text.RegularExpressions;
using Xunit;

namespace Pleiades.Search.Test
{
    public sealed class QuickSearchBuilderTest
    {
        private static readonly Regex _wsRegex = new(@"\s+");
        private static readonly Compiler _compiler = new PostgresCompiler();

        [Fact]
        public void Build_TextOnly_Ok()
        {
            QuickSearchBuilder builder = new();
            var t = builder.Build(new QuickSearchRequest
            {
                Text = "Epidaurus"
            });
            SqlResult result = _compiler.Compile(t.Item1);
            string sql = _wsRegex.Replace(result.ToString(), " ");
            Assert.Equal(
            "SELECT \"place\".\"id\", \"place\".\"title\", " +
            "\"place\".\"rp_lat\", \"place\".\"rp_lon\", " +
            "\"place_meta\".\"value\" AS \"type\" FROM (" +
            "SELECT DISTINCT \"place\".\"id\" " +
            "FROM \"eix_token\" " +
            "INNER JOIN \"eix_occurrence\" ON \"eix_occurrence\".\"token_id\" = \"eix_token\".\"id\" " +
            "INNER JOIN \"place\" ON \"eix_occurrence\".\"target_id\" = \"place\".\"id\" " +
            "WHERE eix_occurrence.field in ('plttl','lcttl','nmrmz') " +
            "AND \"eix_token\".\"value\" = 'epidaurus') AS \"t\" " +
            "INNER JOIN \"place\" ON \"place\".\"id\" = \"t\".\"id\" " +
            "LEFT JOIN \"place_meta\" ON \"place_meta\".\"place_id\" = \"t\".\"id\" " +
            "WHERE \"place_meta\".\"name\" = 'place-type-uri' " +
            "ORDER BY \"place\".\"title\", \"place\".\"id\" LIMIT 20",
            sql);
        }
    }
}
