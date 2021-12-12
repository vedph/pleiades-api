using Fusi.Tools.Data;
using Pleiades.Core;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Pleiades.Search
{
    /// <summary>
    /// Base class for <see cref="IQuickSearch"/> implementors.
    /// </summary>
    public abstract class QuickSearch
    {
        private readonly Compiler _compiler;
        private readonly QuickSearchBuilder _builder;

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        protected string ConnectionString { get; }

        /// <summary>
        /// Create a new instance of <see cref="PgSqlQuickSearch"/> class.
        /// </summary>
        /// <param name="connectionString"></param>
        protected QuickSearch(string connectionString)
        {
            ConnectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
            _compiler = new PostgresCompiler();
            _builder = new QuickSearchBuilder();
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <returns>The connection.</returns>
        protected abstract IDbConnection GetConnection();

        private static void ApplyLookupFilter(LookupFilter filter, Query query)
        {
            if (!string.IsNullOrEmpty(filter.Group))
                query.Where("group", filter.Group);

            if (!string.IsNullOrEmpty(filter.Prefix))
                query.WhereStarts("full_name", filter.Prefix);

            if (!string.IsNullOrEmpty(filter.ShortName))
                query.WhereContains("short_name", filter.ShortName);

            if (filter.PageSize > 0)
                query.Skip(filter.GetSkipCount()).Limit(filter.PageSize);
        }

        /// <summary>
        /// Gets the specified page of lookup data. If you set the page size
        /// to 0, all the matching data will be retrieved at once.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// Resulting page.
        /// </returns>
        /// <exception cref="ArgumentNullException">filter</exception>
        public DataPage<LookupEntry> GetLookup(LookupFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            using IDbConnection connection = GetConnection();
            connection.Open();
            QueryFactory qf = new QueryFactory(connection, _compiler);

            Query query = qf.Query("lookup")
                .Select("id", "full_name", "short_name", "group")
                .OrderBy("group", "full_name", "id");
            ApplyLookupFilter(filter, query);

            // if paging we need total and entries
            if (filter.PageSize > 0)
            {
                // get total
                Query totQuery = qf.Query("lookup").AsCount(new[] { "id" });
                ApplyLookupFilter(filter, totQuery);
                dynamic row = totQuery.First();
                int total = (int)row.count;
                if (total == 0)
                {
                    return new DataPage<LookupEntry>(
                        filter.PageNumber, filter.PageSize, 0,
                        Array.Empty<LookupEntry>());
                }

                // get entries
                return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
                    total, query.Get<LookupEntry>().ToList());
            }

            // else we get all the entries at once
            List<LookupEntry> entries = query.Get<LookupEntry>().ToList();
            return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
                entries.Count, entries);
        }

        /// <summary>
        /// Executes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ArgumentNullException">request</exception>
        public DataPage<dynamic> Execute(QuickSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var t = _builder.Build(request);
            using IDbConnection connection = GetConnection();
            QueryFactory qf = new QueryFactory(connection, _compiler);

            // count
            dynamic row = qf.FromQuery(t.Item2).AsCount().First();
            int total = (int)row.count;

            // items
            List<dynamic> places = new List<dynamic>();
            foreach (dynamic place in qf.FromQuery(t.Item1).Get())
                places.Add(place);

            return new DataPage<dynamic>(request.PageNumber,
                request.PageSize, total, places);
        }
    }
}
