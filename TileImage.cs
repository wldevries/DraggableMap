using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DraggableMap;

public record TileId(int X, int Y, int Z);

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class TileImage : Image
{
    public static readonly DependencyProperty IdProperty =
        DependencyProperty.Register("Id", typeof(TileId), typeof(TileImage), new PropertyMetadata(null));

    public TileId Id
    {
        get { return (TileId)GetValue(IdProperty); }
        set { SetValue(IdProperty, value); }
    }

    private string GetDebuggerDisplay()
    {
        return this.Id.ToString();
    }
}
