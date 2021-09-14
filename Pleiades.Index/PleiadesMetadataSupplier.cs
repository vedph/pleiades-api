using Embix.Core;
using SqlKata.Execution;
using System;
using System.Collections.Generic;

namespace Pleiades.Index
{
    /// <summary>
    /// Metadata supplier for the Pleiades index writer.
    /// This supplies the <c>rank</c> metadatum.
    /// </summary>
    /// <seealso cref="IMetadataSupplier" />
    public sealed class PleiadesMetadataSupplier : IMetadataSupplier
    {
        private readonly Dictionary<int, string> _states;
        private readonly Dictionary<int, string> _certainties;
        private readonly QueryFactory _queryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PleiadesMetadataSupplier"/>
        /// class.
        /// </summary>
        /// <param name="queryFactory">The query factory to be used to fetch
        /// lookup data.</param>
        /// <exception cref="ArgumentNullException">queryFactory</exception>
        public PleiadesMetadataSupplier(QueryFactory queryFactory)
        {
            _queryFactory = queryFactory
                ?? throw new ArgumentNullException(nameof(queryFactory));
            _states = new Dictionary<int, string>();
            _certainties = new Dictionary<int, string>();
            LoadReviewStates();
            LoadCertainties();
        }

        private void LoadReviewStates()
        {
            _states.Clear();
            foreach (dynamic record in _queryFactory.Query("lookup")
                .Select("id AS Id", "full_name AS FullName")
                .Where("group", "state").Get())
            {
                _states[record.Id] = record.FullName;
            }
        }

        private void LoadCertainties()
        {
            _certainties.Clear();
            foreach (dynamic record in _queryFactory.Query("lookup")
                .Select("id AS Id", "full_name AS FullName")
                .WhereLike("full_name", "https://pleiades.stoa.org/vocabularies/association-certainty/%").Get())
            {
                string fullName = record.FullName;
                _certainties[record.Id] =
                    fullName.Substring(fullName.LastIndexOf('/') + 1);
            }
        }

        private byte GetRank(string documentId,
            IDictionary<string, object> metadata)
        {
            int rank = 100;

            // reviewStateId: -10 if pending
            int state = (int)metadata["state"];
            bool pending = string.Compare(_states[state], "pending", true) == 0;
            if (pending) rank -= 10;

            switch (documentId)
            {
                case "place":
                    // title: -50 if ?, -10 if starts with *
                    string title = (string)metadata["raw_title"];
                    if (title.IndexOf('?') > -1) rank -= 50;
                    if (title.StartsWith("*")) rank -= 10;
                    break;
                case "location":
                    // certaintyId: -50 if uncertain, -25 if less-certain
                    int certaintyId = (int)metadata["certainty"];
                    switch (_certainties[certaintyId].ToLowerInvariant())
                    {
                        case "uncertain":
                            rank -= 50;
                            break;
                        case "less-certain":
                            rank -= 25;
                            break;
                    }
                    break;
                case "name":
                    // romanized: -50 if ?
                    string romanized = (string)metadata["raw_romanized"];
                    if (romanized.IndexOf('?') > -1) rank -= 50;
                    break;
            }
            return (byte)(rank < 0 ? 0 : rank);
        }

        /// <summary>
        /// Supplies the specified document identifier.
        /// </summary>
        /// <param name="documentId">The document identifier.</param>
        /// <param name="field">The field.</param>
        /// <param name="token">The token.</param>
        /// <param name="metadata">The metadata.</param>
        public void Supply(string documentId, string field, string token,
            IDictionary<string, object> metadata)
        {
            metadata["rank"] = GetRank(documentId, metadata);
        }
    }
}
