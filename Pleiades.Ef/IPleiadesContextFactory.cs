using Embix.Core;

namespace Pleiades.Ef
{
    /// <summary>
    /// Pleiades context and connection factory.
    /// </summary>
    public interface IPleiadesContextFactory : IDbConnectionFactory
    {
        /// <summary>
        /// Gets the DB context.
        /// </summary>
        public PleiadesDbContext GetContext();
    }
}
