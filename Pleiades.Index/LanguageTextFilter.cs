using Embix.Core.Filters;
using Fusi.Tools.Config;
using System;
using System.Text;

namespace Pleiades.Index;

/// <summary>
/// Filter for language codes in name.language. This is empirically defined
/// by looking at these languages, which in most cases happen to be 2/3
/// letters codes, but sometimes are longer, like
/// <c>etruscan-in-latin-characters</c>. To spare space, also because this
/// information is optionally present, and only for names, we adopt this
/// strategy: 1. if length is up to 3 letters, the code is kept (lowercased);
/// 2. if is more than 3 letters but happens to have a dash after the 2nd
/// or 3rd letter, we cut it to the portion before the dash (e.g.
/// <c>ca-valencia</c> just becomes <c>ca</c>); 3. otherwise, we just
/// set the language to <c>xx</c>.
/// </summary>
/// <para>Tag: <c>text-filter.pleiades.language</c>.</para>
/// <seealso cref="ITextFilter" />
[Tag("text-filter.pleiades.language")]
public sealed class LanguageTextFilter : ITextFilter
{
    /// <summary>
    /// Filters the Pleiades language code.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <exception cref="ArgumentNullException">text</exception>
    public void Apply(StringBuilder text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        for (int i = 0; i < Math.Min(text.Length, 4); i++)
        {
            if (char.IsUpper(text[i]))
                text[i] = char.ToLowerInvariant(text[i]);
            if (text[i] == '-' && i > 0)
            {
                text.Remove(i, text.Length - i);
                break;
            }
        }
        if (text.Length > 3)
        {
            text.Clear();
            text.Append("xx");
        }
    }
}
