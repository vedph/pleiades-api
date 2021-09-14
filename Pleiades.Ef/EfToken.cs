using Pleiades.Core;
using System.Collections.Generic;

namespace Pleiades.Ef
{
    public sealed class EfToken : Token
    {
        public List<EfOccurrence> Occurrences { get; set; }
    }
}
