using System.Numerics;

namespace DraggableMap;

public static class VectorExtensions
{
    public static Vector2 ToVector2(this System.Windows.Vector p) => new((float)p.X, (float)p.Y);
}