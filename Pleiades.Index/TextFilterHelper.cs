using Fusi.Text.Unicode;

namespace Pleiades.Index
{
    internal static class TextFilterHelper
    {
        private static UniData _ud;

        public static UniData UniData
        {
            get
            {
                return _ud ?? (_ud = new UniData());
            }
        }
    }
}
