using static DraggableMap.GeoMath;

namespace DraggableMap;

public record GeoRectangle(double North, double South, double West, double East)
{
    public double GetWidth() => this.East - this.West;
    public double GetHeight() => this.North - this.South;

    public GeoCoordinate GetNorthWest() => new(North, West);
    public GeoCoordinate GetNorthEast() => new(North, East);
    public GeoCoordinate GetSouthWest() => new(South, West);
    public GeoCoordinate GetSouthEast() => new(South, East);

    public GeoCoordinate GetCenter()
    {
        const int tileSize = 256;
        var nwPx = PositionToGlobalPixel(this.GetNorthWest(), 0, tileSize);
        var sePx = PositionToGlobalPixel(this.GetSouthEast(), 0, tileSize);

        var centerPx = (nwPx + sePx) / 2;
        return GlobalPixelToPosition(centerPx, 0, tileSize);
    }

    // Warning: does not support bounds across the antimeridean (180 longitude)
    public static GeoRectangle From(GeoCoordinate c1, GeoCoordinate c2)
    {
        var north = Math.Max(c1.Latitude, c2.Latitude);
        var south = Math.Min(c1.Latitude, c2.Latitude);
        var west = Math.Min(c1.Longitude, c2.Longitude);
        var east = Math.Max(c1.Longitude, c2.Longitude);
        return new(north, south, west, east);
    }

    // Warning: does not support bounds across the antimeridean (180 longitude)
    public static GeoRectangle? From(IReadOnlyCollection<GeoCoordinate> coordinates)
    {
        if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
        if (coordinates.Count == 0) return null;

        var north = coordinates.Max(c => c.Latitude);
        var south = coordinates.Min(c => c.Latitude);
        var west = coordinates.Min(c => c.Longitude);
        var east = coordinates.Max(c => c.Longitude);
        return new(north, south, west, east);
    }

    public GeoRectangle Merge(GeoRectangle other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return From([this.GetNorthWest(), this.GetSouthEast(), other.GetNorthWest(), other.GetSouthEast()])!;
    }

    public bool Contains(GeoCoordinate pt)
    {
        if (pt == null)
        {
            return false;
        }
        return pt.Latitude < this.North &&
               pt.Longitude > this.West &&
               pt.Latitude > this.South &&
               pt.Longitude < this.East;
    }

    public GeoRectangle Scale(double scale)
    {
        var center = this.GetCenter();
        var width = this.GetWidth() * scale;
        var height = this.GetHeight() * scale;
        return new GeoRectangle(
            center.Latitude + height / 2,
            center.Latitude - height / 2,
            center.Longitude - width / 2,
            center.Longitude + width / 2);
    }
}