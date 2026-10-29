using Embix.Search;
using Fusi.Tools.Data;
using Pleiades.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Search;

/// <summary>
/// Base class for <see cref="IQuickSearch"/> implementors. Each method
/// opens (and closes) its own pooled connection, so that this object is
/// thread-safe and can be used as a singleton.
/// </summary>
public abstract class QuickSearch : IQuickSearch
{
    private const string PLACE_DETAIL_SQL =
        // place
        "SELECT p.id, p.uri, p.title, p.description, p.details, " +
        "p.provenance, p.rights, ls.full_name, p.created, p.modified, " +
        "p.rp_lat, p.rp_lon, " +
        "p.bbox_sw_lon, p.bbox_sw_lat, p.bbox_ne_lon, p.bbox_ne_lat\n" +
        "FROM place p LEFT JOIN lookup ls ON ls.id=p.review_state_id\n" +
        "WHERE p.id=@id;\n" +
        // metadata (types, subjects)
        "SELECT \"name\", value FROM place_meta WHERE place_id=@id " +
        "ORDER BY id;\n" +
        // names
        "SELECT n.id, n.uri, n.romanized, n.attested, n.\"language\", " +
        "lt.full_name, lc.full_name, la.full_name, lp.full_name, " +
        "n.start_year, n.end_year, n.description\n" +
        "FROM \"name\" n\n" +
        "LEFT JOIN lookup lt ON lt.id=n.type_id\n" +
        "LEFT JOIN lookup lc ON lc.id=n.certainty_id\n" +
        "LEFT JOIN lookup la ON la.id=n.tr_accuracy_id\n" +
        "LEFT JOIN lookup lp ON lp.id=n.tr_completeness_id\n" +
        "WHERE n.place_id=@id ORDER BY n.id;\n" +
        // locations
        "SELECT l.id, l.uri, l.title, lc.full_name, la.full_name, " +
        "l.accuracy_value, l.start_year, l.end_year, l.description, " +
        "COALESCE(ST_AsGeoJSON(l.geo), NULLIF(l.geometry, 'null'))\n" +
        "FROM \"location\" l\n" +
        "LEFT JOIN lookup lc ON lc.id=l.certainty_id\n" +
        "LEFT JOIN lookup la ON la.id=l.accuracy_id\n" +
        "WHERE l.place_id=@id ORDER BY l.id;\n" +
        // outgoing connections
        "SELECT c.id, c.target_id, tp.title, lt.full_name, lc.full_name, " +
        "c.title, c.start_year, c.end_year\n" +
        "FROM \"connection\" c\n" +
        "LEFT JOIN place tp ON tp.id=c.target_id\n" +
        "LEFT JOIN lookup lt ON lt.id=c.type_id\n" +
        "LEFT JOIN lookup lc ON lc.id=c.certainty_id\n" +
        "WHERE c.source_id=@id ORDER BY c.id;\n" +
        // incoming connections
        "SELECT c.id, c.source_id, sp.title, lt.full_name, lc.full_name, " +
        "c.title, c.start_year, c.end_year\n" +
        "FROM \"connection\" c\n" +
        "LEFT JOIN place sp ON sp.id=c.source_id\n" +
        "LEFT JOIN lookup lt ON lt.id=c.type_id\n" +
        "LEFT JOIN lookup lc ON lc.id=c.certainty_id\n" +
        "WHERE c.target_id=@id ORDER BY c.id;\n" +
        // linked places
        "SELECT pl.target_id, tp.title FROM place_link pl\n" +
        "LEFT JOIN place tp ON tp.id=pl.target_id\n" +
        "WHERE pl.source_id=@id ORDER BY tp.title, pl.target_id;";

    private SqlDialect? _dialect;
    private QuickSearchBuilder? _builder;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    protected string ConnectionString { get; }

    /// <summary>
    /// Gets or sets the URI prefix of places, used in results.
    /// </summary>
    public string UriPrefix { get; set; } = "https://pleiades.stoa.org/places/";

    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public SqlDialect Dialect => _dialect ??= CreateDialect();

    /// <summary>
    /// Gets the query builder.
    /// </summary>
    public QuickSearchBuilder Builder => _builder ??= new QuickSearchBuilder(Dialect);

    /// <summary>
    /// Create a new instance of the <see cref="QuickSearch"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <exception cref="ArgumentNullException">connectionString</exception>
    protected QuickSearch(string connectionString)
    {
        ConnectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Gets a new connection, not yet opened.
    /// </summary>
    /// <returns>The connection.</returns>
    protected abstract DbConnection GetConnection();

    /// <summary>
    /// Creates the SQL dialect.
    /// </summary>
    /// <returns>Dialect.</returns>
    protected abstract SqlDialect CreateDialect();

    #region Helpers
    private async Task<DbConnection> OpenAsync(CancellationToken cancel)
    {
        DbConnection connection = GetConnection();
        try
        {
            await connection.OpenAsync(cancel).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> CountAsync(DbConnection connection,
        SqlQuery query, CancellationToken cancel)
    {
        await using DbCommand cmd = query.CreateCommand(connection);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancel)
            .ConfigureAwait(false));
    }

    private static async Task<List<T>> QueryAsync<T>(DbConnection connection,
        SqlQuery query, Func<IDataRecord, T> map, CancellationToken cancel)
    {
        await using DbCommand cmd = query.CreateCommand(connection);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancel)
            .ConfigureAwait(false);
        List<T> items = [];
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
            items.Add(map(reader));
        return items;
    }

    // empty strings (frequent in the source data) are returned as null
    private static string? GetString(IDataRecord record, int i)
    {
        if (record.IsDBNull(i)) return null;
        string s = record.GetString(i);
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static double? GetDouble(IDataRecord record, int i) =>
        record.IsDBNull(i) ? null : Convert.ToDouble(record.GetValue(i));

    private static int GetInt(IDataRecord record, int i) =>
        Convert.ToInt32(record.GetValue(i));

    private static (int? Start, int? End) GetYears(IDataRecord record, int i)
    {
        int start = GetInt(record, i), end = GetInt(record, i + 1);
        // 0-0 means unknown
        return start == 0 && end == 0 ? (null, null) : (start, end);
    }

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
    #endregion

    /// <summary>
    /// Maps the current record of a places query to a result. The record
    /// has fields id, title, lat, lon, types, distance.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>The result.</returns>
    protected virtual QuickSearchResult MapResult(IDataRecord record)
    {
        string[] types = record.IsDBNull(4)
            ? [] : (string[])record.GetValue(4);

        return new QuickSearchResult
        {
            Id = record.GetString(0),
            UriPrefix = UriPrefix,
            Name = GetString(record, 1),
            Lat = GetDouble(record, 2),
            Lng = GetDouble(record, 3),
            Types = types,
            Type = types.Length > 0 ? types[0] : null,
            Distance = GetDouble(record, 5)
        };
    }

    /// <summary>
    /// Gets the specified page of lookup data. If you set the page size
    /// to 0, all the matching data will be retrieved at once.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Resulting page.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public async Task<DataPage<LookupEntry>> GetLookupAsync(LookupFilter filter,
        CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        PagedSqlQuery query = Builder.BuildLookup(filter);
        await using DbConnection connection = await OpenAsync(cancel)
            .ConfigureAwait(false);

        int total = await CountAsync(connection, query.Count, cancel)
            .ConfigureAwait(false);
        List<LookupEntry> entries = total == 0 ? [] :
            await QueryAsync(connection, query.Data, r => new LookupEntry
            {
                Id = r.GetInt32(0),
                FullName = GetString(r, 1),
                ShortName = GetString(r, 2),
                Group = GetString(r, 3)
            }, cancel).ConfigureAwait(false);

        return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
            total, entries);
    }

    /// <summary>
    /// Executes the specified request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Resulting page.</returns>
    /// <exception cref="ArgumentNullException">request</exception>
    public async Task<DataPage<QuickSearchResult>> ExecuteAsync(
        QuickSearchRequest request, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PagedSqlQuery query = Builder.Build(request);
        await using DbConnection connection = await OpenAsync(cancel)
            .ConfigureAwait(false);

        int total = await CountAsync(connection, query.Count, cancel)
            .ConfigureAwait(false);
        List<QuickSearchResult> places = total == 0 ? [] :
            await QueryAsync(connection, query.Data, MapResult, cancel)
                .ConfigureAwait(false);

        return new DataPage<QuickSearchResult>(request.PageNumber,
            request.PageSize, total, places);
    }

    /// <summary>
    /// Gets the places nearest to the specified place, sorted by distance.
    /// </summary>
    /// <param name="placeId">The reference place ID.</param>
    /// <param name="distanceMax">The maximum distance in meters, or 0
    /// for no limit.</param>
    /// <param name="limit">The maximum count of places to get.</param>
    /// <param name="placeType">The optional place type(s).</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>Places, or null if the reference place was not found
    /// or is not located.</returns>
    /// <exception cref="ArgumentNullException">placeId</exception>
    public async Task<IList<QuickSearchResult>?> GetNearbyAsync(
        string placeId, int distanceMax, int limit, string? placeType = null,
        CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(placeId);

        await using DbConnection connection = await OpenAsync(cancel)
            .ConfigureAwait(false);

        SqlParameters parameters = new(Dialect);
        int located = await CountAsync(connection, new SqlQuery(
            "SELECT COUNT(*) FROM place WHERE id=" +
            $"{parameters.Add(placeId)} AND geo_pt IS NOT NULL;", parameters),
            cancel).ConfigureAwait(false);
        if (located == 0) return null;

        return await QueryAsync(connection,
            Builder.BuildNearby(placeId, distanceMax, limit, placeType),
            MapResult, cancel).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the details of the specified place.
    /// </summary>
    /// <param name="id">The place ID.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The place, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">id</exception>
    public async Task<PlaceDetail?> GetPlaceAsync(string id,
        CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await using DbConnection connection = await OpenAsync(cancel)
            .ConfigureAwait(false);
        SqlParameters parameters = new(Dialect);
        parameters.Add("id", id);

        // a single roundtrip with multiple result sets
        await using DbCommand cmd = new SqlQuery(PLACE_DETAIL_SQL, parameters)
            .CreateCommand(connection);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancel)
            .ConfigureAwait(false);

        // place
        if (!await reader.ReadAsync(cancel).ConfigureAwait(false)) return null;
        bool located = !(reader.GetDouble(10) == 0 && reader.GetDouble(11) == 0);
        PlaceDetail place = new()
        {
            Id = reader.GetString(0),
            Uri = GetString(reader, 1),
            Title = GetString(reader, 2),
            Description = GetString(reader, 3),
            Details = GetString(reader, 4),
            Provenance = GetString(reader, 5),
            Rights = GetString(reader, 6),
            ReviewState = GetString(reader, 7),
            Created = reader.GetDateTime(8),
            Modified = reader.GetDateTime(9),
            Lat = located ? reader.GetDouble(10) : null,
            Lng = located ? reader.GetDouble(11) : null,
            BBox = located && !reader.IsDBNull(12) && !reader.IsDBNull(13)
                && !reader.IsDBNull(14) && !reader.IsDBNull(15)
                ? [reader.GetDouble(12), reader.GetDouble(13),
                   reader.GetDouble(14), reader.GetDouble(15)]
                : null
        };

        // metadata
        await reader.NextResultAsync(cancel).ConfigureAwait(false);
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            switch (GetString(reader, 0))
            {
                case "place-type-uri":
                    place.Types.Add(reader.GetString(1));
                    break;
                case "subject":
                    place.Subjects.Add(reader.GetString(1));
                    break;
            }
        }

        // names
        await reader.NextResultAsync(cancel).ConfigureAwait(false);
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            var (start, end) = GetYears(reader, 9);
            place.Names.Add(new PlaceDetailName
            {
                Id = reader.GetInt32(0),
                Uri = GetString(reader, 1),
                Romanized = GetString(reader, 2),
                Attested = GetString(reader, 3),
                Language = GetString(reader, 4),
                Type = GetString(reader, 5),
                Certainty = GetString(reader, 6),
                TranscriptionAccuracy = GetString(reader, 7),
                TranscriptionCompleteness = GetString(reader, 8),
                StartYear = start,
                EndYear = end,
                Description = GetString(reader, 11)
            });
        }

        // locations
        await reader.NextResultAsync(cancel).ConfigureAwait(false);
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            var (start, end) = GetYears(reader, 6);
            place.Locations.Add(new PlaceDetailLocation
            {
                Id = reader.GetInt32(0),
                Uri = GetString(reader, 1),
                Title = GetString(reader, 2),
                Certainty = GetString(reader, 3),
                Accuracy = GetString(reader, 4),
                AccuracyValue = reader.GetDouble(5),
                StartYear = start,
                EndYear = end,
                Description = GetString(reader, 8),
                Geometry = ParseJson(GetString(reader, 9))
            });
        }

        // connections
        foreach (IList<PlaceDetailConnection> target in new[]
            { place.Connections, place.IncomingConnections })
        {
            await reader.NextResultAsync(cancel).ConfigureAwait(false);
            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
            {
                var (start, end) = GetYears(reader, 6);
                target.Add(new PlaceDetailConnection
                {
                    Id = reader.GetInt32(0),
                    Place = new PlaceDetailRef
                    {
                        Id = reader.GetString(1),
                        Title = GetString(reader, 2)
                    },
                    Type = GetString(reader, 3),
                    Certainty = GetString(reader, 4),
                    Title = GetString(reader, 5),
                    StartYear = start,
                    EndYear = end
                });
            }
        }

        // linked places
        await reader.NextResultAsync(cancel).ConfigureAwait(false);
        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
        {
            place.LinkedPlaces.Add(new PlaceDetailRef
            {
                Id = reader.GetString(0),
                Title = GetString(reader, 1)
            });
        }

        return place;
    }
}
