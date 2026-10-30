using Pleiades.Search;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PleiadesApi.Models;

/// <summary>
/// Spatial filters.
/// </summary>
public class SpatialBindingModel : PagingBindingModel, IValidatableObject
{
    /// <summary>
    /// The minimum distance in meters from the distance point. 0=no minimum.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DistanceMin { get; set; }

    /// <summary>
    /// The maximum distance in meters from the distance point. 0=no maximum.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DistanceMax { get; set; }

    /// <summary>
    /// The longitude of the distance point. This point is used for distance
    /// filters and for sorting by distance; when set, results include their
    /// distance from it.
    /// </summary>
    [Range(-180, 180)]
    public double? DistanceLon { get; set; }

    /// <summary>
    /// The latitude of the distance point.
    /// </summary>
    [Range(-90, 90)]
    public double? DistanceLat { get; set; }

    /// <summary>
    /// The longitude of the bounding box SW corner. When the box crosses
    /// the antimeridian, this is greater than the NE corner's longitude
    /// (e.g. 170 for a box from 170 to -170).
    /// </summary>
    [Range(-180, 180)]
    public double? SwCornerLon { get; set; }

    /// <summary>
    /// The latitude of the bounding box SW corner.
    /// </summary>
    [Range(-90, 90)]
    public double? SwCornerLat { get; set; }

    /// <summary>
    /// The longitude of the bounding box NE corner.
    /// </summary>
    [Range(-180, 180)]
    public double? NeCornerLon { get; set; }

    /// <summary>
    /// The latitude of the bounding box NE corner.
    /// </summary>
    [Range(-90, 90)]
    public double? NeCornerLat { get; set; }

    /// <summary>
    /// True if the bounding box must fully contain the place's bounding box,
    /// rather than just intersect it (which is the default behavior).
    /// </summary>
    public bool ContainedInBox { get; set; }

    /// <summary>
    /// The polygonal region, as a list of <c>lon,lat</c> vertices separated
    /// by semicolons (at least 3, at most 200; the ring is closed
    /// automatically). Longitudes can be in the range -540 to 540, so that
    /// a polygon crossing the antimeridian can have continuous coordinates
    /// (e.g. 170 to 190); its longitude span must be less than 360.
    /// </summary>
    [MaxLength(8000)]
    public string? Region { get; set; }

    /// <summary>
    /// True if the region must fully contain the place's bounding box,
    /// rather than just intersect it (which is the default behavior).
    /// </summary>
    public bool ContainedInRegion { get; set; }

    /// <summary>
    /// Gets the region vertices parsed from <see cref="Region"/>, or null
    /// if not set or invalid.
    /// </summary>
    public IList<SearchRequestPoint>? GetRegion() =>
        SpatialRegion.Parse(Region, out _);

    /// <summary>
    /// Gets a value indicating whether the distance point is set.
    /// </summary>
    public bool HasDistancePoint => DistanceLon != null && DistanceLat != null;

    /// <summary>
    /// Gets a value indicating whether the bounding box is set.
    /// </summary>
    public bool HasBBox => SwCornerLon != null && SwCornerLat != null
        && NeCornerLon != null && NeCornerLat != null;

    /// <summary>
    /// Validates the consistency of the spatial filters.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>Validation results.</returns>
    public virtual IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if ((DistanceLon == null) != (DistanceLat == null))
        {
            yield return new ValidationResult(
                "Both longitude and latitude of the distance point are required",
                [nameof(DistanceLon), nameof(DistanceLat)]);
        }

        if ((DistanceMin > 0 || DistanceMax > 0) && !HasDistancePoint)
        {
            yield return new ValidationResult(
                "Distance filters require a distance point",
                [nameof(DistanceMin), nameof(DistanceMax)]);
        }

        if (DistanceMin > 0 && DistanceMax > 0 && DistanceMin > DistanceMax)
        {
            yield return new ValidationResult(
                "Minimum distance is greater than maximum distance",
                [nameof(DistanceMin), nameof(DistanceMax)]);
        }

        bool anyCorner = SwCornerLon != null || SwCornerLat != null
            || NeCornerLon != null || NeCornerLat != null;
        if (anyCorner && !HasBBox)
        {
            yield return new ValidationResult(
                "Bounding box requires both its SW and NE corners",
                [nameof(SwCornerLon), nameof(SwCornerLat),
                 nameof(NeCornerLon), nameof(NeCornerLat)]);
        }

        // longitudes are not compared, as a box crossing the antimeridian
        // has its W edge east of its E edge
        if (HasBBox && SwCornerLat > NeCornerLat)
        {
            yield return new ValidationResult(
                "The SW corner of the bounding box must be at the south " +
                "of its NE corner",
                [nameof(SwCornerLat), nameof(NeCornerLat)]);
        }

        if (!string.IsNullOrWhiteSpace(Region))
        {
            SpatialRegion.Parse(Region, out string? error);
            if (error != null)
                yield return new ValidationResult(error, [nameof(Region)]);
        }
    }
}
