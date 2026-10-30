using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Pleiades.Search.Test;

public sealed class SpatialRegionTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_Empty_Null(string? text)
    {
        Assert.Null(SpatialRegion.Parse(text, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void Parse_Valid_Ok()
    {
        IList<SearchRequestPoint>? points = SpatialRegion.Parse(
            " 23,37.5; 24.2 ,37.5;23.6,38.5 ", out string? error);

        Assert.Null(error);
        Assert.NotNull(points);
        Assert.Equal(3, points.Count);
        Assert.Equal(24.2, points[1].Longitude);
        Assert.Equal(37.5, points[1].Latitude);
    }

    [Fact]
    public void Parse_ClosingAndDuplicateVertices_Removed()
    {
        IList<SearchRequestPoint>? points = SpatialRegion.Parse(
            "23,37.5;24.2,37.5;24.2,37.5;23.6,38.5;23,37.5", out _);

        Assert.NotNull(points);
        Assert.Equal(3, points.Count);
    }

    [Theory]
    [InlineData("23,37.5;24.2", "Invalid region vertex")]
    [InlineData("23,37.5;x,1;2,3", "Invalid region vertex")]
    [InlineData("23,37.5;24,37.5", "at least 3")]
    [InlineData("23,37.5;24,37.5;23,37.5", "at least 3")]
    [InlineData("1,1;2,91;3,1", "latitudes")]
    [InlineData("1,1;541,1;3,2", "longitudes")]
    [InlineData("-180,1;0,1;180,2", "less than 360")]
    public void Parse_Invalid_Error(string text, string expected)
    {
        Assert.Null(SpatialRegion.Parse(text, out string? error));
        Assert.NotNull(error);
        Assert.Contains(expected, error);
    }

    [Fact]
    public void Parse_TooManyVertices_Error()
    {
        List<string> pairs = [];
        for (int i = 0; i <= SpatialRegion.MaxVertices; i++)
            pairs.Add(string.Create(CultureInfo.InvariantCulture,
                $"{i * 0.1},{i % 2}"));

        Assert.Null(SpatialRegion.Parse(string.Join(";", pairs),
            out string? error));
        Assert.Contains("at most", error);
    }

    [Fact]
    public void Parse_CrossingAntimeridian_Ok()
    {
        IList<SearchRequestPoint>? points = SpatialRegion.Parse(
            "170,10;190,10;180,20", out string? error);

        Assert.Null(error);
        Assert.True(SpatialRegion.NeedsWrap(points!));
    }

    [Fact]
    public void FormatAndToWkt_Ok()
    {
        List<SearchRequestPoint> points =
        [
            new(23, 37.5),
            new(-24.25, 37.5),
            new(23.6, 38.5)
        ];

        Assert.Equal("23,37.5;-24.25,37.5;23.6,38.5",
            SpatialRegion.Format(points));
        Assert.Equal("POLYGON((23 37.5,-24.25 37.5,23.6 38.5,23 37.5))",
            SpatialRegion.ToWkt(points));
        Assert.False(SpatialRegion.NeedsWrap(points));
    }
}
