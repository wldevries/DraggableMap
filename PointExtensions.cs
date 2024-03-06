using System.Numerics;
using System.Windows;

namespace DraggableMap;

public static class PointExtensions
{
    public static System.Windows.Vector ToVector(this Point p) => new(p.X, p.Y);
    public static Vector2 ToVector2(this Point p) => new((float)p.X, (float)p.Y);
}