using System.IO;
using System.Windows.Media.Imaging;

namespace DraggableMap;

public static class TileFetcher
{
    public const int TileSize = 256;

    private const string Root = @"E:\Dev\dnd\Grafelgam\svelte-typescript-app\app\public\tiles\";

    private static readonly Dictionary<TileId, BitmapImage> tiles = [];

    public static BitmapImage GetTile(TileId id)
    {
        if (tiles.TryGetValue(id, out var image))
        {
            return image;
        }

        string path = Path.Combine(Root, id.Z.ToString(), id.X.ToString(), id.Y.ToString() + ".png");
        Uri uri = new(path);
        BitmapImage newImage = new(uri)
        {
            CacheOption = BitmapCacheOption.OnLoad,
        };
        tiles[id] = newImage;
        return newImage;
    }

    public static int GetWidth(int z)
    {
        return (int)Math.Pow(2, z);
    }
}
