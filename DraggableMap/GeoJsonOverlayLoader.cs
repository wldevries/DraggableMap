using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace DraggableMap;

public static class GeoJsonOverlayLoader
{
    private static readonly (Color Stroke, Color Fill)[] Palette =
    [
        (Color.FromRgb(230, 57, 70), Color.FromArgb(70, 230, 57, 70)),
        (Color.FromRgb(29, 78, 216), Color.FromArgb(70, 29, 78, 216)),
        (Color.FromRgb(22, 163, 74), Color.FromArgb(70, 22, 163, 74)),
        (Color.FromRgb(217, 119, 6), Color.FromArgb(70, 217, 119, 6)),
    ];

    public static IReadOnlyCollection<GeoJsonOverlayViewModel> LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<GeoJsonOverlayViewModel> overlays = [];
        var files = Directory
            .GetFiles(directory, "*.geojson", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        int colorIndex = 0;
        foreach (var file in files)
        {
            var paths = ParseGeoJson(file);
            if (paths.Count == 0)
            {
                continue;
            }

            var color = Palette[colorIndex % Palette.Length];
            overlays.Add(new GeoJsonOverlayViewModel(
                Path.GetFileNameWithoutExtension(file),
                paths,
                color.Stroke,
                color.Fill));

            colorIndex++;
        }

        return overlays;
    }

    private static IReadOnlyList<GeoPath> ParseGeoJson(string filePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        List<GeoPath> paths = [];
        ParseElement(document.RootElement, paths);
        return paths;
    }

    private static void ParseElement(JsonElement element, List<GeoPath> paths)
    {
        if (!element.TryGetProperty("type", out var typeProperty))
        {
            return;
        }

        var type = typeProperty.GetString();
        switch (type)
        {
            case "FeatureCollection":
                if (element.TryGetProperty("features", out var features))
                {
                    foreach (var feature in features.EnumerateArray())
                    {
                        ParseElement(feature, paths);
                    }
                }
                break;

            case "Feature":
                if (element.TryGetProperty("geometry", out var geometry) &&
                    geometry.ValueKind == JsonValueKind.Object)
                {
                    ParseElement(geometry, paths);
                }
                break;

            case "GeometryCollection":
                if (element.TryGetProperty("geometries", out var geometries))
                {
                    foreach (var geom in geometries.EnumerateArray())
                    {
                        ParseElement(geom, paths);
                    }
                }
                break;

            case "Polygon":
                if (element.TryGetProperty("coordinates", out var polygon))
                {
                    ParsePolygonCoordinates(polygon, paths);
                }
                break;

            case "MultiPolygon":
                if (element.TryGetProperty("coordinates", out var multiPolygon))
                {
                    foreach (var polygonRings in multiPolygon.EnumerateArray())
                    {
                        ParsePolygonCoordinates(polygonRings, paths);
                    }
                }
                break;

            case "LineString":
                if (element.TryGetProperty("coordinates", out var lineString))
                {
                    AddPath(paths, ParseCoordinateList(lineString), false);
                }
                break;

            case "MultiLineString":
                if (element.TryGetProperty("coordinates", out var multiLineString))
                {
                    foreach (var line in multiLineString.EnumerateArray())
                    {
                        AddPath(paths, ParseCoordinateList(line), false);
                    }
                }
                break;
        }
    }

    private static void ParsePolygonCoordinates(JsonElement polygonCoordinates, List<GeoPath> paths)
    {
        foreach (var ring in polygonCoordinates.EnumerateArray())
        {
            AddPath(paths, ParseCoordinateList(ring), true);
        }
    }

    private static void AddPath(List<GeoPath> paths, IReadOnlyList<GeoCoordinate> points, bool isClosed)
    {
        if (points.Count < 2)
        {
            return;
        }

        List<GeoCoordinate> normalized = [.. points];

        if (isClosed && normalized[0] != normalized[^1])
        {
            normalized.Add(normalized[0]);
        }

        paths.Add(new GeoPath(normalized, isClosed));
    }

    private static IReadOnlyList<GeoCoordinate> ParseCoordinateList(JsonElement coordinates)
    {
        List<GeoCoordinate> result = [];

        foreach (var item in coordinates.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
            {
                continue;
            }

            var lngElement = item[0];
            var latElement = item[1];

            if (!lngElement.TryGetDouble(out var lng) || !latElement.TryGetDouble(out var lat))
            {
                continue;
            }

            result.Add(new GeoCoordinate(lat, lng));
        }

        return result;
    }
}
