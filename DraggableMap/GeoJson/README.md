# GeoJSON Overlays

Place your `.geojson` files in this directory to display them as overlays on the map.

## Features

- Files are loaded alphabetically
- Each file gets a unique color from the palette (red, blue, green, orange, then repeating)
- Supported geometry types:
  - Polygon
  - MultiPolygon
  - LineString
  - MultiLineString
  - FeatureCollection
  - GeometryCollection

## Example File Structure

```
GeoJson/
  ??? area1.geojson
  ??? area2.geojson
  ??? routes.geojson
```

## Adding New Overlays

1. Drop your `.geojson` file into this directory
2. Rebuild/restart the application
3. The overlay will appear below the pins on the map

## Example GeoJSON (Polygon)

```json
{
  "type": "Feature",
  "geometry": {
    "type": "Polygon",
    "coordinates": [[
      [5.0, 52.0],
      [6.0, 52.0],
      [6.0, 53.0],
      [5.0, 53.0],
      [5.0, 52.0]
    ]]
  },
  "properties": {
    "name": "Example Area"
  }
}
```

Note: Coordinates are in [longitude, latitude] format (standard GeoJSON).
