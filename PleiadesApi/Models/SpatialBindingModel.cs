using System;
using System.ComponentModel.DataAnnotations;

namespace PleiadesApi.Models
{
    /// <summary>
    /// Spatial filters.
    /// </summary>
    public class SpatialBindingModel : PagingBindingModel
    {
        /// <summary>
        /// Gets or sets the minimum distance from the distance point,
        /// expressed in meters.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int DistanceMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum distance from the distance point,
        /// expressed in meters.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int DistanceMax { get; set; }

        /// <summary>
        /// The longitude.
        /// </summary>
        [Range(-180, 180)]
        public double? DistanceLon{ get; set; }

        /// <summary>
        /// The latitude.
        /// </summary>
        [Range(-90, 90)]
        public double? DistanceLat { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the bounding box SW corner.
        /// </summary>
        [Range(-180, 180)]
        public double? SwCornerLon { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the bounding box SW corner.
        /// </summary>
        [Range(-90, 90)]
        public double? SwCornerLat { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the bounding box NE corner.
        /// </summary>
        [Range(-180, 180)]
        public double? NeCornerLon { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the bounding box NE corner.
        /// </summary>
        [Range(-90, 90)]
        public double? NeCornerLat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bounding box should
        /// fully contain the target place, rather than just intersect with
        /// any part of it (which is the default behavior).
        /// </summary>
        public bool ContainedInBox { get; set; }
    }
}
