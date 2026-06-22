using System.Numerics;

namespace DraggableMap;

public static class GeoMath
{
    public const double MinLatitude = -85.05112878;
    public const double MaxLatitude = 85.05112878;
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;

    public static Vector2 ProjectToMap(GeoCoordinate coor, GeoRectangle mapBounds, double pixelWidth, double pixelHeight)
    {
        Vector2 pixelNorthWest = projectOntoMap(mapBounds.GetNorthWest());
        Vector2 pixelSouthEast = projectOntoMap(mapBounds.GetSouthEast());
        var localWidth = pixelSouthEast.X - pixelNorthWest.X;
        var localHeight = pixelSouthEast.Y - pixelNorthWest.Y;
        if (localWidth != 0 && localHeight != 0)
        {
            var widthScale = pixelWidth / localWidth;
            var heightScale = pixelHeight / localHeight;
            Vector2 pixelInWorld = projectOntoMap(coor);
            var xp = pixelInWorld.X - pixelNorthWest.X;
            var yp = pixelInWorld.Y - pixelNorthWest.Y;
            return new Vector2((float)(xp * widthScale), (float)(yp * heightScale));
        }
        else
        {
            return projectOntoMap(coor);
        }

        static Vector2 projectOntoMap(GeoCoordinate coor)
        {
            var sinLat = Math.Sin(coor.Latitude * Math.PI / 180);
            var pixelX = (coor.Longitude + 180) / 360;
            var pixelY = 0.5 - (Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI));
            return new Vector2((float)pixelX, (float)pixelY);
        }
    }

    /// <summary>
    /// Global Converts a Pixel coordinate into a geospatial coordinate at a specified zoom level. 
    /// Global Pixel coordinates are relative to the top left corner of the map (90, -180)
    /// </summary>
    /// <param name="pixel">Pixel coordinates in the format of [x, y].</param>  
    /// <param name="zoom">Zoom level</param>
    /// <param name="tileSize">The size of the tiles in the tile pyramid.</param>
    /// <returns>A position value in the format [longitude, latitude].</returns>
    public static GeoCoordinate GlobalPixelToPosition(Vector2 pixel, float zoom, int tileSize)
    {
        var mapSize = MapSize(zoom, tileSize);

        var x = (Clip(pixel.X, 0, mapSize - 1) / mapSize) - 0.5f;
        var y = 0.5 - (Clip(pixel.Y, 0, mapSize - 1) / mapSize);

        var longitude = 360 * x;
        var latitude = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;

        return new(latitude, longitude);
    }

    /// <summary>
    /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees) into pixel XY coordinates at a specified level of detail.
    /// </summary>
    /// <param name="position">Position coordinate in the format [longitude, latitude]</param>
    /// <param name="zoom">Zoom level.</param>
    /// <param name="tileSize">The size of the tiles in the tile pyramid.</param> 
    /// <returns>A global pixel coordinate.</returns>
    public static Vector2 PositionToGlobalPixel(GeoCoordinate position, int zoom, int tileSize)
    {
        var latitude = Clip(position.Latitude, MinLatitude, MaxLatitude);
        var longitude = Clip(position.Longitude, MinLongitude, MaxLongitude);

        var x = (longitude + 180) / 360;
        var sinLatitude = Math.Sin(latitude * Math.PI / 180);
        var y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

        var mapSize = MapSize(zoom, tileSize);

        return new(
            (float)Clip(x * mapSize, 0, mapSize - 1),
            (float)Clip(y * mapSize, 0, mapSize - 1));
    }

    /// <summary>
    /// Clips a number to the specified minimum and maximum values.
    /// </summary>
    /// <param name="n">The number to clip.</param>
    /// <param name="minValue">Minimum allowable value.</param>
    /// <param name="maxValue">Maximum allowable value.</param>
    /// <returns>The clipped value.</returns>
    private static double Clip(double n, double minValue, double maxValue)
    {
        return Math.Min(Math.Max(n, minValue), maxValue);
    }

    /// <summary>
    /// Calculates width and height of the map in pixels at a specific zoom level from -180 degrees to 180 degrees.
    /// </summary>
    /// <param name="zoom">Zoom Level to calculate width at</param>
    /// <param name="tileSize">The size of the tiles in the tile pyramid.</param>
    /// <returns>Width and height of the map in pixels</returns>
    public static double MapSize(double zoom, int tileSize)
    {
        return Math.Ceiling(tileSize * Math.Pow(2, zoom));
    }
}
