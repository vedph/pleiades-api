using Microsoft.Extensions.Logging;
using Pleiades.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Pleiades.Migration;

/// <summary>
/// JSON-file place reader.
/// This reads the full Pleiades JSON dump one place at a time from its
/// @graph object.
/// </summary>
public sealed class JsonPlaceReader
{
    private readonly JsonSerializerOptions _jwOptions;
    private readonly JsonElement _graphEl;
    private int _currentIndex;
    private bool _eof;

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets the ordinal number of the last-read place (1-N).
    /// </summary>
    public int Position => _currentIndex + 1;

    /// <summary>
    /// Gets the total number of places in the graph being read.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the lookup set.
    /// </summary>
    public LookupEntrySet LookupSet { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPlaceReader"/> class.
    /// </summary>
    /// <param name="stream">The JSON stream.</param>
    /// <param name="lookupSet">The lookup set or null to create a new one.
    /// </param>
    /// <exception cref="ArgumentNullException">stream</exception>
    public JsonPlaceReader(Stream stream, LookupEntrySet? lookupSet)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        LookupSet = lookupSet ?? new LookupEntrySet();
        _jwOptions = new JsonSerializerOptions { WriteIndented = false };

        // root object has @context{}, @graph[]: move to @graph's first item
        var doc = JsonDocument.Parse(stream,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            });
        _graphEl = doc.RootElement.GetProperty("@graph");
        _currentIndex = -1;
        Length = _graphEl.GetArrayLength();
    }

    #region Helpers
    private void ReadCoords(JsonElement element, Place place)
    {
        // reprPoint[]
        double[]? coords = element.GetDoubleArrayValue("reprPoint");
        if (coords != null)
        {
            if (coords.Length != 2)
            {
                Logger?.LogError(
                    $"Expected reprPoint length (2) not met: {coords.Length}");
            }
            else
            {
                place.RpLon = coords[0];
                place.RpLat = coords[1];
            }
        }

        // bbox[]
        coords = element.GetDoubleArrayValue("bbox");
        if (coords != null)
        {
            if (coords.Length != 4)
            {
                Logger?.LogError(
                    $"Expected bbox length (4) not met: {coords.Length}");
            }
            else
            {
                place.BboxSwLat = coords[1];
                place.BboxSwLon = coords[0];
                place.BboxNeLat = coords[3];
                place.BboxNeLon = coords[2];
            }
        }
    }

    private static Author[] ReadAuthors(JsonElement element)
    {
        Author[] authors = new Author[element.GetArrayLength()];
        for (int i = 0; i < authors.Length; i++)
        {
            JsonElement item = element[i];
            authors[i] = new Author
            {
                Id = item.GetStringPropertyValue("username"),
                Name = item.GetStringPropertyValue("name"),
                Homepage = item.GetStringPropertyValue("homepage")
            };
        }
        return authors;
    }

    //private static string CamelToKebab(string text)
    //{
    //    if (string.IsNullOrEmpty(text)) return text;

    //    StringBuilder sb = new StringBuilder();
    //    foreach (char c in text)
    //    {
    //        if (char.IsUpper(c) && sb.Length > 0) sb.Append('-');
    //        sb.Append(char.ToLower(c));
    //    }
    //    return sb.ToString();
    //}

    private IList<Metadata> ReadPairs(JsonElement element,
        string vn, string un, string mn)
    {
        List<Metadata> metadata = new();
        string[]? types = element.GetStringArrayValue(vn);
        string[]? typeUris = element.GetStringArrayValue(un);

        if (types?.Length != typeUris?.Length)
        {
            Logger?.LogError($"{vn} and {un} lengths not equal");
        }
        else
        {
            // add URI to metadata, save value into lookup
            for (int i = 0; i < types?.Length; i++)
            {
                string uri = typeUris![i];
                LookupSet.GetId(uri, types[i]);
                metadata.Add(new Metadata(mn, uri));
            }
        }

        return metadata;
    }

    private string ReadJson(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Undefined
            ? ""
            : JsonSerializer.Serialize(element, _jwOptions);
    }

    /// <summary>
    /// Reads the URI and value from the specified properties, if any,
    /// updates the prefixes dictionary with them, and returns them.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="uriName">Name of the URI.</param>
    /// <param name="valueName">Name of the value.</param>
    /// <returns>Tuple with URI and value, or null if not found.</returns>
    private Tuple<string, string>? ReadUriAndValue(JsonElement element,
        string uriName, string valueName)
    {
        string? uri = element.GetStringPropertyValue(uriName);
        string? value = element.GetStringPropertyValue(valueName);
        if (string.IsNullOrEmpty(uri) && string.IsNullOrEmpty(value))
            return null;

        if ((uri != null && value == null) ||
            (uri == null && value != null))
        {
            Logger?.LogError("Expected URI/value pair for " +
                $"{uriName}/{valueName} not found");
        }
        LookupSet.GetId(uri, value);
        return Tuple.Create(uri!, value!);
    }

    private static DateTime? ReadModifiedFromHistory(JsonElement element)
    {
        if (element.GetArrayLength() == 0) return null;
        return element[0].GetDatePropertyValue("modified");
    }

    private Attestation[] ReadAttestations(JsonElement element)
    {
        Attestation[] attestations = new Attestation[element.GetArrayLength()];
        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            var tPeriod = ReadUriAndValue(item, "timePeriodURI", "timePeriod");
            var tConfidence = ReadUriAndValue(item, "confidenceURI", "confidence");

            attestations[i++] = new Attestation
            {
                PeriodId = tPeriod?.Item1,
                ConfidenceId = tConfidence?.Item2
            };
        }
        return attestations;
    }

    private static Reference[] ReadReferences(JsonElement element)
    {
        Reference[] references = new Reference[element.GetArrayLength()];
        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            references[i++] = new Reference
            {
                Title = item.GetStringPropertyValue("shortTitle"),
                Type = item.GetStringPropertyValue("type"),
                CitTypeUri = item.GetStringPropertyValue("citationTypeURI"),
                AccessUri = item.GetStringPropertyValue("accessURI"),
                AlternateUri = item.GetStringPropertyValue("alternateURI"),
                BibUri = item.GetStringPropertyValue("bibliographicURI"),
                Citation = item.GetStringPropertyValue("formattedCitation"),
                CitationDetail = item.GetStringPropertyValue("citationDetail"),
                OtherId = item.GetStringPropertyValue("otherIdentifier")
            };
        }
        return references;
    }

    private PlaceFeature[] ReadFeatures(JsonElement element)
    {
        PlaceFeature[] features = new PlaceFeature[element.GetArrayLength()];
        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            PlaceFeature feature = new()
            {
                Type = item.GetStringPropertyValue("type"),
            };

            // geometry: JSON
            if (item.TryGetProperty("geometry", out JsonElement gElem))
                feature.Geometry = ReadJson(gElem);

            // properties/snippet, link, description, location_precision, title
            if (item.TryGetProperty("properties", out JsonElement propElem))
            {
                feature.Snippet = propElem.GetStringPropertyValue("snippet");
                feature.Link = propElem.GetStringPropertyValue("link");
                feature.Description = propElem.GetStringPropertyValue("description");
                feature.Precision = propElem.GetStringPropertyValue("location_precision");
                feature.Title = propElem.GetStringPropertyValue("title");
            }

            features[i++] = feature;
        }

        return features;
    }

    private Location[] ReadLocations(JsonElement element)
    {
        Location[] locations = new Location[element.GetArrayLength()];
        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            string? uri = item.GetStringPropertyValue("associationCertaintyURI");
            string? value = item.GetStringPropertyValue("associationCertainty");
            if ((uri != null && value == null) || (value != null && uri == null))
            {
                Logger?.LogError(
                    $"Location certainty URI \"{uri}\" not in synch with " +
                    $"value \"{value}\"");
            }
            else LookupSet.GetId(uri, value);

            Location location = new()
            {
                CertaintyId = uri,
                Uri = item.GetStringPropertyValue("uri"),
                StartYear = (short)item.GetIntPropertyValue("start"),
                EndYear = (short)item.GetIntPropertyValue("end"),
                Title = item.GetStringPropertyValue("title"),
                Provenance = item.GetStringPropertyValue("provenance"),
                Remains = item.GetStringPropertyValue("archaeologicalRemains"),
                Details = item.GetStringPropertyValue("details"),
                ReviewState = item.GetStringPropertyValue("review_state")
                    ?.ToLowerInvariant(),
                AccuracyId = item.GetStringPropertyValue("accuracy"),
                AccuracyValue = item.GetDoublePropertyValue("accuracy_value"),
                Description = item.GetStringPropertyValue("description"),
                Created = item.GetDatePropertyValue("created"),
            };

            // geometry: JSON
            if (item.TryGetProperty("geometry", out JsonElement gElem))
                location.Geometry = ReadJson(gElem);

            // featureType[], featureTypeURI[]
            location.Metadata.AddRange(
                ReadPairs(item, "featureType", "featureTypeURI", "feature-type-uri"));

            // locationType[], locationTypeURI[]
            location.Metadata.AddRange(
                ReadPairs(item, "locationType", "locationTypeURI", "location-type-uri"));

            // creators[]
            if (item.TryGetProperty("creators", out JsonElement crElem))
                location.Creators.AddRange(ReadAuthors(crElem));

            // history[]: we assume that the 1st item has the latest date
            if (item.TryGetProperty("history", out JsonElement histElem))
            {
                DateTime? dt = ReadModifiedFromHistory(histElem);
                if (dt != null) location.Modified = dt.Value;
            }

            locations[i++] = location;
        }

        return locations;
    }

    private Connection[] ReadConnections(JsonElement element)
    {
        Connection[] connections = new Connection[element.GetArrayLength()];

        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            var tType = ReadUriAndValue(item, "connectionTypeURI", "connectionType");

            Connection connection = new()
            {
                Uri = item.GetStringPropertyValue("uri"),
                TypeId = tType?.Item1,
                Title = item.GetStringPropertyValue("title"),
                Description = item.GetStringPropertyValue("description"),
                StartYear = (short)item.GetIntPropertyValue("start"),
                EndYear = (short)item.GetIntPropertyValue("end"),
                Details = item.GetStringPropertyValue("details"),
                Provenance = item.GetStringPropertyValue("provenance"),
                Certainty = item.GetStringPropertyValue("associationCertainty"),
                TargetUri = item.GetStringPropertyValue("connectsTo"),
                ReviewState = item.GetStringPropertyValue("review_state")
                    ?.ToLowerInvariant(),
                Created = item.GetDatePropertyValue("created")
            };

            // attestations[], references[]
            if (item.TryGetProperty("attestations", out JsonElement attElem))
                connection.Attestations.AddRange(ReadAttestations(attElem));

            if (item.TryGetProperty("references", out JsonElement refElem))
                connection.References.AddRange(ReadReferences(refElem));

            // creators[], contributors[]
            if (item.TryGetProperty("creators", out JsonElement crElem))
                connection.Creators.AddRange(ReadAuthors(crElem));

            if (item.TryGetProperty("contributors", out JsonElement coElem))
                connection.Contributors.AddRange(ReadAuthors(coElem));

            // history[]: we assume that the 1st item has the latest date
            if (item.TryGetProperty("history", out JsonElement histElem))
            {
                DateTime? dt = ReadModifiedFromHistory(histElem);
                if (dt != null) connection.Modified = dt.Value;
            }

            connections[i++] = connection;
        }

        return connections;
    }

    private Name[] ReadNames(JsonElement element)
    {
        Name[] names = new Name[element.GetArrayLength()];
        int i = 0;
        foreach (JsonElement item in element.EnumerateArray())
        {
            var tCertainty = ReadUriAndValue(
                item, "associationCertaintyURI", "associationCertainty");

            Name name = new()
            {
                Uri = item.GetStringPropertyValue("uri"),
                Language = item.GetStringPropertyValue("language"),
                StartYear = (short)item.GetIntPropertyValue("start"),
                EndYear = (short)item.GetIntPropertyValue("end"),
                Type = item.GetStringPropertyValue("nameType"),
                Attested = item.GetStringPropertyValue("attested"),
                Romanized = item.GetStringPropertyValue("romanized"),
                Provenance = item.GetStringPropertyValue("provenance"),
                Description = item.GetStringPropertyValue("description"),
                Details = item.GetStringPropertyValue("details"),
                TrAccuracy = item.GetStringPropertyValue("transcriptionAccuracy"),
                TrCompleteness = item.GetStringPropertyValue("transcriptionCompleteness"),
                CertaintyId = tCertainty?.Item1,
                ReviewState = item.GetStringPropertyValue("review_state")
                    ?.ToLowerInvariant(),
                Created = item.GetDatePropertyValue("created")
            };

            // attestations[], references[]
            if (item.TryGetProperty("attestations", out JsonElement attElem))
                name.Attestations.AddRange(ReadAttestations(attElem));

            if (item.TryGetProperty("references", out JsonElement refElem))
                name.References.AddRange(ReadReferences(refElem));

            // creators[], contributors[]
            if (item.TryGetProperty("creators", out JsonElement crElem))
                name.Creators.AddRange(ReadAuthors(crElem));

            if (item.TryGetProperty("contributors", out JsonElement coElem))
                name.Contributors.AddRange(ReadAuthors(coElem));

            // history[]: we assume that the 1st item has the latest date
            if (item.TryGetProperty("history", out JsonElement histElem))
            {
                DateTime? dt = ReadModifiedFromHistory(histElem);
                if (dt != null) name.Modified = dt.Value;
            }

            names[i++] = name;
        }

        return names;
    }
    #endregion

    /// <summary>
    /// Reads the next place.
    /// </summary>
    /// <returns>Place or null if end of document reached.</returns>
    public Place? Read()
    {
        if (_eof) return null;

        if (++_currentIndex >= Length)
        {
            _eof = true;
            return null;
        }

        JsonElement element = _graphEl[_currentIndex];
        Place place = new()
        {
            Id = element.GetStringPropertyValue("id"),
            Uri = element.GetStringPropertyValue("uri"),
            Type = element.GetStringPropertyValue("type"),
            Title = element.GetStringPropertyValue("title"),
            Description = element.GetStringPropertyValue("description"),
            Details = element.GetStringPropertyValue("details"),
            Provenance = element.GetStringPropertyValue("provenance"),
            Rights = element.GetStringPropertyValue("rights"),
            ReviewState = element.GetStringPropertyValue("review_state")
                ?.ToLowerInvariant(),
            Created = element.GetDatePropertyValue("created"),
        };

        Logger?.LogInformation($"{place.Id}: {place.Title}");

        // reprPoint[], bbox[]
        ReadCoords(element, place);

        // subject[]
        string[]? subjects = element.GetStringArrayValue("subject");
        if (subjects?.Length > 0)
        {
            place.Metadata.AddRange(
                subjects.Select(s => new Metadata("subject", s)));
        }

        // placeTypes[], placeTypeURIs[]
        place.Metadata.AddRange(
            ReadPairs(element, "placeTypes", "placeTypeURIs", "place-type-uri"));

        // connectsWith[]
        string[]? targetUris = element.GetStringArrayValue("connectsWith");
        if (targetUris?.Length > 0) place.TargetUris.AddRange(targetUris);

        // history[]: we assume that the 1st item has the latest date
        if (element.TryGetProperty("history", out JsonElement histElem))
        {
            DateTime? dt = ReadModifiedFromHistory(histElem);
            if (dt != null) place.Modified = dt.Value;
        }

        // creators[], contributors[]
        if (element.TryGetProperty("creators", out JsonElement crElem))
            place.Creators.AddRange(ReadAuthors(crElem));
        if (element.TryGetProperty("contributors", out JsonElement coElem))
            place.Contributors.AddRange(ReadAuthors(coElem));

        // attestations[], references[]
        if (element.TryGetProperty("attestations", out JsonElement attElem))
            place.Attestations.AddRange(ReadAttestations(attElem));
        if (element.TryGetProperty("references", out JsonElement refElem))
            place.References.AddRange(ReadReferences(refElem));

        // features[]
        if (element.TryGetProperty("features", out JsonElement featElem))
            place.Features.AddRange(ReadFeatures(featElem));

        // locations[]
        if (element.TryGetProperty("locations", out JsonElement locElem))
            place.Locations.AddRange(ReadLocations(locElem));

        // connections[]
        if (element.TryGetProperty("connections", out JsonElement connElem))
            place.Connections.AddRange(ReadConnections(connElem));

        // names[]
        if (element.TryGetProperty("names", out JsonElement namElem))
            place.Names.AddRange(ReadNames(namElem));

        return place;
    }
}
