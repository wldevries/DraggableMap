using ReactiveUI;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace DraggableMap;

public sealed record GeoPath(IReadOnlyList<GeoCoordinate> Coordinates, bool IsClosed);

public class GeoJsonOverlayViewModel : ReactiveObject
{
    private Geometry geometry = Geometry.Empty;
    private bool isVisible = true;
    private readonly IReadOnlyList<GeoPath> paths;

    public GeoJsonOverlayViewModel(
        string name,
        IReadOnlyList<GeoPath> paths,
        Color stroke,
        Color fill)
    {
        this.Name = name;
        this.paths = paths;

        SolidColorBrush strokeBrush = new(stroke);
        strokeBrush.Freeze();
        this.Stroke = strokeBrush;

        SolidColorBrush fillBrush = new(fill);
        fillBrush.Freeze();
        this.Fill = fillBrush;
    }

    public string Name { get; }

    public Brush Stroke { get; }

    public Brush Fill { get; }

    public double StrokeThickness { get; } = 2;

    public bool IsVisible
    {
        get => this.isVisible;
        set => this.RaiseAndSetIfChanged(ref this.isVisible, value);
    }

    public Geometry Geometry
    {
        get => this.geometry;
        private set => this.RaiseAndSetIfChanged(ref this.geometry, value);
    }

    public void Update(GeoRectangle bounds, Size resolution, Vector2 offset)
    {
        if (resolution.Width <= 0 || resolution.Height <= 0 || this.paths.Count == 0)
        {
            this.Geometry = Geometry.Empty;
            return;
        }

        StreamGeometry stream = new();

        using (var context = stream.Open())
        {
            foreach (var path in this.paths)
            {
                if (path.Coordinates.Count < 2)
                {
                    continue;
                }

                List<Point> points = new(path.Coordinates.Count);
                foreach (var coordinate in path.Coordinates)
                {
                    var projected = GeoMath.ProjectToMap(coordinate, bounds, resolution.Width, resolution.Height);
                    points.Add(new Point(projected.X + offset.X, projected.Y + offset.Y));
                }

                context.BeginFigure(points[0], path.IsClosed, path.IsClosed);

                if (points.Count > 1)
                {
                    List<Point> tail = new(points.Count - 1);
                    for (int i = 1; i < points.Count; i++)
                    {
                        tail.Add(points[i]);
                    }

                    context.PolyLineTo(tail, true, true);
                }
            }
        }

        stream.Freeze();
        this.Geometry = stream;
    }
}
