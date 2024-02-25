using System.Windows;

namespace DraggableMap;

public static class PointExtensions
{
    public static Vector ToVector(this Point p) => new(p.X, p.Y);
}