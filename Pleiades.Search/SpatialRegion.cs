using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Pleiades.Search;

/// <summary>
/// Helper for polygonal regions used as spatial filters (see
/// <see cref="QuickSearchSpatial.Region"/>). A region is a list of vertices
/// in WGS84 longitude and latitude, with no closing vertex. Its text form is
/// a list of <c>lon,lat</c> pairs separated by semicolons, e.g.
/// <c>23,37.5;24.2,37.5;23.6,38.5</c>. Longitudes may exceed the -180/180
/// range, so that a polygon crossing the antimeridian can be expressed with
/// continuous coordinates.
/// </summary>
public static class SpatialRegion
{
    /// <summary>
    /// The maximum count of vertices in a region.
    /// </summary>
    public const int MaxVertices = 200;

    /// <summary>
    /// The maximum absolute value of a vertex longitude.
    /// </summary>
    public const double MaxLongitude = 540;

    /// <summary>
    /// Parses the specified text into the vertices of a region. A closing
    /// vertex equal to the first one, and consecutive duplicate vertices,
    /// are removed.
    /// </summary>
    /// <param name="text">The text, with semicolon-separated <c>lon,lat</c>
    /// pairs.</param>
    /// <param name="error">The parsing or validation error, if any.</param>
    /// <returns>The vertices, or null if the text is empty or invalid.
    /// </returns>
    public static IList<SearchRequestPoint>? Parse(string? text,
        out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text)) return null;

        List<SearchRequestPoint> points = [];
        foreach (string pair in text.Split(';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] values = pair.Split(',', StringSplitOptions.TrimEntries);
            if (values.Length != 2
                || !double.TryParse(values[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lon)
                || !double.TryParse(values[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lat)
                || !double.IsFinite(lon) || !double.IsFinite(lat))
            {
                error = $"Invalid region vertex: \"{pair}\" " +
                    "(expected longitude,latitude)";
                return null;
            }
            points.Add(new SearchRequestPoint(lon, lat));
            if (points.Count > MaxVertices + 1) break;
        }

        points = Normalize(points);
        error = Validate(points);
        return error == null ? points : null;
    }

    private static bool AreEqual(SearchRequestPoint a, SearchRequestPoint b) =>
        a.Longitude == b.Longitude && a.Latitude == b.Latitude;

    /// <summary>
    /// Removes consecutive duplicate vertices, and the closing vertex.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>The normalized vertices.</returns>
    public static List<SearchRequestPoint> Normalize(
        IEnumerable<SearchRequestPoint> points)
    {
        List<SearchRequestPoint> result = [];
        foreach (SearchRequestPoint p in points)
        {
            if (result.Count == 0 || !AreEqual(result[^1], p)) result.Add(p);
        }
        while (result.Count > 1 && AreEqual(result[0], result[^1]))
            result.RemoveAt(result.Count - 1);
        return result;
    }

    /// <summary>
    /// Validates the specified region vertices.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>The error message, or null if valid.</returns>
    public static string? Validate(IList<SearchRequestPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < 3)
            return "A region requires at least 3 distinct vertices";
        if (points.Count > MaxVertices)
            return $"A region can have at most {MaxVertices} vertices";
        if (points.Any(p => p.Latitude < -90 || p.Latitude > 90))
            return "Region latitudes must be between -90 and 90";
        if (points.Any(p => Math.Abs(p.Longitude) > MaxLongitude))
        {
            return "Region longitudes must be between " +
                $"-{MaxLongitude} and {MaxLongitude}";
        }
        if (points.Max(p => p.Longitude) - points.Min(p => p.Longitude) >= 360)
            return "A region must span less than 360 degrees of longitude";
        return null;
    }

    /// <summary>
    /// Formats the specified vertices into their text form.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>Text.</returns>
    public static string Format(IEnumerable<SearchRequestPoint> points) =>
        string.Join(";", points.Select(p => string.Create(
            CultureInfo.InvariantCulture, $"{p.Longitude},{p.Latitude}")));

    /// <summary>
    /// Builds the WKT polygon for the specified vertices, closing its ring.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>WKT.</returns>
    public static string ToWkt(IList<SearchRequestPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        StringBuilder sb = new("POLYGON((");
        foreach (SearchRequestPoint p in points.Append(points[0]))
        {
            if (sb[^1] != '(') sb.Append(',');
            sb.Append(p.Longitude.ToString(CultureInfo.InvariantCulture))
              .Append(' ')
              .Append(p.Latitude.ToString(CultureInfo.InvariantCulture));
        }
        return sb.Append("))").ToString();
    }

    /// <summary>
    /// Determines whether the specified vertices have longitudes outside
    /// the -180/180 range, i.e. the region must be wrapped around the
    /// antimeridian.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>True if wrapping is required.</returns>
    public static bool NeedsWrap(IEnumerable<SearchRequestPoint> points) =>
        points.Any(p => p.Longitude < -180 || p.Longitude > 180);
}
