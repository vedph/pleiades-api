using Embix.Core;
using System;
using System.Collections.Generic;
using System.Data;

namespace Pleiades.Index;

/// <summary>
/// Metadata supplier for the Pleiades index writer.
/// This supplies the <c>rank</c> metadatum. Once created, it is thread-safe,
/// as it only reads its lookup data.
/// </summary>
/// <seealso cref="IMetadataSupplier" />
public sealed class PleiadesMetadataSupplier : IMetadataSupplier
{
    private const string CERTAINTY_PREFIX =
        "https://pleiades.stoa.org/vocabularies/association-certainty/";

    private readonly Dictionary<int, string> _states;
    private readonly Dictionary<int, string> _certainties;

    /// <summary>
    /// Initializes a new instance of the <see cref="PleiadesMetadataSupplier"/>
    /// class.
    /// </summary>
    /// <param name="connFactory">The connection factory to be used to fetch
    /// lookup data.</param>
    /// <exception cref="ArgumentNullException">connFactory</exception>
    public PleiadesMetadataSupplier(IDbConnectionFactory connFactory)
    {
        ArgumentNullException.ThrowIfNull(connFactory);

        _states = [];
        _certainties = [];

        using IDbConnection connection = connFactory.GetConnection();
        connection.Open();
        LoadReviewStates(connection);
        LoadCertainties(connection);
    }

    private static void LoadLookup(IDbConnection connection, string sql,
        Action<int, string> add)
    {
        using IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) add(reader.GetInt32(0), reader.GetString(1));
    }

    private void LoadReviewStates(IDbConnection connection)
    {
        LoadLookup(connection,
            "SELECT id, full_name FROM lookup WHERE \"group\"='state';",
            (id, name) => _states[id] = name);
    }

    private void LoadCertainties(IDbConnection connection)
    {
        LoadLookup(connection,
            "SELECT id, full_name FROM lookup " +
            $"WHERE full_name LIKE '{CERTAINTY_PREFIX}%';",
            (id, name) => _certainties[id] = name[(name.LastIndexOf('/') + 1)..]);
    }

    private static int? GetInt(IDictionary<string, object> metadata, string key)
    {
        return metadata.TryGetValue(key, out object? value) && value is not null
            ? Convert.ToInt32(value)
            : null;
    }

    private static string GetString(IDictionary<string, object> metadata,
        string key)
    {
        return metadata.TryGetValue(key, out object? value)
            ? value as string ?? ""
            : "";
    }

    private byte GetRank(string documentId, IDictionary<string, object> metadata)
    {
        int rank = 100;

        // reviewStateId: -10 if not published (e.g. drafting, pending)
        int? state = GetInt(metadata, "state");
        if (state != null && _states.TryGetValue(state.Value, out string? s)
            && !string.Equals(s, "published", StringComparison.OrdinalIgnoreCase))
        {
            rank -= 10;
        }

        switch (documentId)
        {
            case "place":
                // title: -50 if ?, -10 if starts with *
                string title = GetString(metadata, "raw_title");
                if (title.Contains('?')) rank -= 50;
                if (title.StartsWith('*')) rank -= 10;
                break;
            case "location":
                // certaintyId: -50 if uncertain, -25 if less-certain
                int? certaintyId = GetInt(metadata, "certainty");
                if (certaintyId != null
                    && _certainties.TryGetValue(certaintyId.Value, out string? c))
                {
                    switch (c.ToLowerInvariant())
                    {
                        case "uncertain":
                            rank -= 50;
                            break;
                        case "less-certain":
                            rank -= 25;
                            break;
                    }
                }
                break;
            case "name":
                // romanized: -50 if ?
                if (GetString(metadata, "raw_romanized").Contains('?'))
                    rank -= 50;
                break;
        }
        return (byte)(rank < 0 ? 0 : rank);
    }

    /// <summary>
    /// Supplies the rank metadatum.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="field">The field.</param>
    /// <param name="token">The token.</param>
    /// <param name="metadata">The metadata.</param>
    public void Supply(string documentId, string field, string token,
        IDictionary<string, object>? metadata)
    {
        if (metadata == null) return;
        metadata["rank"] = GetRank(documentId, metadata);
    }
}
