using System.Text;
using Xunit;

namespace Pleiades.Index.Test
{
    public sealed class LanguageTextFilterTest
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("akk", "akk")]
        [InlineData("AKK", "akk")]
        [InlineData("ca-valencia", "ca")]
        [InlineData("grc-Latn", "grc")]
        [InlineData("etruscan-in-latin-characters", "xx")]
        public void Apply(string text, string expected)
        {
            LanguageTextFilter filter = new LanguageTextFilter();
            StringBuilder sb = new StringBuilder(text);
            filter.Apply(sb);
            Assert.Equal(expected, sb.ToString());
        }
    }
}
