using System.Globalization;

namespace DraggableMap;

public record GeoCoordinate(double Latitude, double Longitude)
{
    /// <summary>
    /// Parse ISO 6709 annex H format
    /// </summary>
    public static GeoCoordinate? ParseIso(string? location)
    {
        if (location is null || string.IsNullOrWhiteSpace(location))
        {
            return null;
        }
        else
        {
            try
            {
                location = location.Replace("/", "");
                int splitIndex = Math.Max(location.LastIndexOf("+"), location.LastIndexOf("-"));
                if (splitIndex != -1)
                {
                    double latitude = double.Parse(location[..splitIndex], CultureInfo.InvariantCulture);
                    double longitude = double.Parse(location[splitIndex..], CultureInfo.InvariantCulture);
                    return new(latitude, longitude);
                }
            }
            catch (Exception)
            {
            }
        }
        return null;
    }

    /// <summary>
    /// Parse standard format
    /// </summary>
    public static GeoCoordinate? Parse(string? location)
    {
        if (location is null || string.IsNullOrWhiteSpace(location))
        {
            return null;
        }
        else
        {
            try
            {
                int splitIndex = location.LastIndexOf(",");
                if (splitIndex != -1)
                {
                    double latitude = double.Parse(location[..splitIndex], CultureInfo.InvariantCulture);
                    double longitude = double.Parse(location[(splitIndex + 1)..], CultureInfo.InvariantCulture);
                    return new(latitude, longitude);
                }
            }
            catch (Exception)
            {
            }
        }
        return null;
    }

    public override string ToString()
    {
        return FormattableString.Invariant($"{Latitude},{Longitude}");
    }
}