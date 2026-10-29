using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pleiades.Search;

/// <summary>
/// The details of a place, with its names, locations and connections.
/// Lookup values (types, certainties, etc.) are resolved to their full
/// names (usually URIs).
/// </summary>
public class PlaceDetail
{
    /// <summary>The place ID.</summary>
    public string Id { get; set; } = "";

    /// <summary>The place URI.</summary>
    public string? Uri { get; set; }

    /// <summary>The place title.</summary>
    public string? Title { get; set; }

    /// <summary>The description.</summary>
    public string? Description { get; set; }

    /// <summary>The details.</summary>
    public string? Details { get; set; }

    /// <summary>The provenance.</summary>
    public string? Provenance { get; set; }

    /// <summary>The rights.</summary>
    public string? Rights { get; set; }

    /// <summary>The review state.</summary>
    public string? ReviewState { get; set; }

    /// <summary>The creation date.</summary>
    public DateTime Created { get; set; }

    /// <summary>The last modification date.</summary>
    public DateTime Modified { get; set; }

    /// <summary>The representative point latitude, or null if not located.
    /// </summary>
    public double? Lat { get; set; }

    /// <summary>The representative point longitude, or null if not located.
    /// </summary>
    public double? Lng { get; set; }

    /// <summary>The bounding box as [swLng, swLat, neLng, neLat], or null
    /// if not located.</summary>
    [JsonPropertyName("bbox")]
    public double[]? BBox { get; set; }

    /// <summary>The place types URIs.</summary>
    public IList<string> Types { get; set; } = [];

    /// <summary>The subjects.</summary>
    public IList<string> Subjects { get; set; } = [];

    /// <summary>The names.</summary>
    public IList<PlaceDetailName> Names { get; set; } = [];

    /// <summary>The locations.</summary>
    public IList<PlaceDetailLocation> Locations { get; set; } = [];

    /// <summary>The connections from this place to other places.</summary>
    public IList<PlaceDetailConnection> Connections { get; set; } = [];

    /// <summary>The connections from other places to this place.</summary>
    public IList<PlaceDetailConnection> IncomingConnections { get; set; } = [];

    /// <summary>The places this place is linked to (<c>connectsWith</c>).
    /// </summary>
    public IList<PlaceDetailRef> LinkedPlaces { get; set; } = [];
}

/// <summary>
/// A reference to a place.
/// </summary>
public class PlaceDetailRef
{
    /// <summary>The place ID.</summary>
    public string Id { get; set; } = "";

    /// <summary>The place title.</summary>
    public string? Title { get; set; }
}

/// <summary>
/// A place's name.
/// </summary>
public class PlaceDetailName
{
    /// <summary>The internal ID.</summary>
    public int Id { get; set; }

    /// <summary>The name URI.</summary>
    public string? Uri { get; set; }

    /// <summary>The romanized form(s).</summary>
    public string? Romanized { get; set; }

    /// <summary>The attested form, in its original script.</summary>
    public string? Attested { get; set; }

    /// <summary>The language code.</summary>
    public string? Language { get; set; }

    /// <summary>The name type.</summary>
    public string? Type { get; set; }

    /// <summary>The association certainty.</summary>
    public string? Certainty { get; set; }

    /// <summary>The transcription accuracy.</summary>
    public string? TranscriptionAccuracy { get; set; }

    /// <summary>The transcription completeness.</summary>
    public string? TranscriptionCompleteness { get; set; }

    /// <summary>The start year, or null if unknown.</summary>
    public int? StartYear { get; set; }

    /// <summary>The end year, or null if unknown.</summary>
    public int? EndYear { get; set; }

    /// <summary>The description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// A place's location.
/// </summary>
public class PlaceDetailLocation
{
    /// <summary>The internal ID.</summary>
    public int Id { get; set; }

    /// <summary>The location URI.</summary>
    public string? Uri { get; set; }

    /// <summary>The title.</summary>
    public string? Title { get; set; }

    /// <summary>The association certainty.</summary>
    public string? Certainty { get; set; }

    /// <summary>The accuracy assessment.</summary>
    public string? Accuracy { get; set; }

    /// <summary>The accuracy value in meters.</summary>
    public double AccuracyValue { get; set; }

    /// <summary>The start year, or null if unknown.</summary>
    public int? StartYear { get; set; }

    /// <summary>The end year, or null if unknown.</summary>
    public int? EndYear { get; set; }

    /// <summary>The description.</summary>
    public string? Description { get; set; }

    /// <summary>The GeoJSON geometry, or null.</summary>
    public JsonElement? Geometry { get; set; }
}

/// <summary>
/// A connection between two places.
/// </summary>
public class PlaceDetailConnection
{
    /// <summary>The internal ID.</summary>
    public int Id { get; set; }

    /// <summary>The connected place (target for outgoing connections,
    /// source for incoming ones).</summary>
    public PlaceDetailRef Place { get; set; } = new();

    /// <summary>The connection type.</summary>
    public string? Type { get; set; }

    /// <summary>The association certainty.</summary>
    public string? Certainty { get; set; }

    /// <summary>The title.</summary>
    public string? Title { get; set; }

    /// <summary>The start year, or null if unknown.</summary>
    public int? StartYear { get; set; }

    /// <summary>The end year, or null if unknown.</summary>
    public int? EndYear { get; set; }
}
