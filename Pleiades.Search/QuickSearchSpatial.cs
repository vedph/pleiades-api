using System.Text;

namespace Pleiades.Search
{
    /// <summary>
    /// Spatial filters used in <see cref="QuickSearchRequest"/>.
    /// </summary>
    public class QuickSearchSpatial
    {
        /// <summary>
        /// Gets or sets the minimum distance from <see cref="DistancePoint"/>,
        /// expressed in meters.
        /// </summary>
        public int DistanceMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum distance from <see cref="DistancePoint"/>,
        /// expressed in meters.
        /// </summary>
        public int DistanceMax { get; set; }

        /// <summary>
        /// Gets or sets the distance point coordinates.
        /// </summary>
        public SearchRequestPoint DistancePoint { get; set; }

        /// <summary>
        /// Gets or sets the bounding box SW corner.
        /// </summary>
        public SearchRequestPoint BBoxSwCorner { get; set; }

        /// <summary>
        /// Gets or sets the bounding box NE corner.
        /// </summary>
        public SearchRequestPoint BBoxNeCorner { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bounding box should
        /// fully contain the target place, rather than just intersect with
        /// any part of it (which is the default behavior).
        /// </summary>
        public bool IsBBoxContainer { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new();

            if (DistancePoint != null)
            {
                sb.Append('[').Append(DistanceMin).Append(" - ")
                    .Append(DistanceMax).Append("] from ").Append(DistancePoint);
            }

            if (BBoxSwCorner != null && BBoxNeCorner != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(BBoxSwCorner)
                    .Append(" / ").Append(BBoxNeCorner)
                    .Append(' ').Append(IsBBoxContainer ? "contains" : "intersects");
            }

            return sb.ToString();
        }
    }
}
