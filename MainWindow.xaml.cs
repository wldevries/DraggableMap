using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DraggableMap;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int MinZoom = 0;
    private const int MaxZoom = 14;

    public static readonly DependencyProperty TopLeftProperty =
        DependencyProperty.Register("TopLeft", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(3834, 2563), TopLeftChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register("ZoomLevel", typeof(int), typeof(MainWindow), new PropertyMetadata(5));

    private bool isPanning = false;
    private Point mousePosition = new();
    private TileFetcher TileFetcher = new();
    private bool isUpdating = false;
    private ObservableCollection<PinViewModel> pins = new();

    public MainWindow()
    {
        InitializeComponent();

        PinViewModel.Load();

        this.Loaded += (_, _) => this.UpdateCanvas();
        this.SizeChanged += (_, _) => this.UpdateCanvas();

        this.MouseDown += MainWindow_MouseDown;
        this.MouseMove += TileCanvas_MouseMove;
        this.MouseUp += TileCanvas_MouseUp;

        this.MouseWheel += MainWindow_MouseWheel;
        this.KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Add)
        {
            this.ZoomIn(this.TopLeft + new Vector(this.ActualWidth / 2, this.ActualHeight / 2));
            e.Handled = true;
        }
        else if (e.Key == Key.Subtract)
        {
            this.ZoomOut(this.TopLeft + new Vector(this.ActualWidth / 2, this.ActualHeight / 2));
            e.Handled = true;
        }
    }

    private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var center = this.TopLeft + e.GetPosition(this.TileCanvas).ToVector();

        if (e.Delta > 1)
        {
            ZoomIn(center);
        }
        else
        {
            ZoomOut(center);
        }
    }

    private async void ZoomOut(Point center)
    {
        if (this.ZoomLevel > MinZoom && !isUpdating)
        {
            this.isUpdating = true;
            this.ZoomLevel--;
            this.TopLeft -= center.ToVector() / 2;
            this.isUpdating = false;
            await this.UpdateCanvas();
        }
    }

    private async void ZoomIn(Point center)
    {
        if (this.ZoomLevel < MaxZoom && !isUpdating)
        {
            this.isUpdating = true;
            this.ZoomLevel++;
            this.TopLeft += center.ToVector();
            this.isUpdating = false;
            await this.UpdateCanvas();
        }
    }

    private void TileCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (isPanning)
        {
            StopPanning();
        }
    }

    private void StopPanning()
    {
        Mouse.Capture(null);
        this.Cursor = null;
        isPanning = false;
    }

    private void TileCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (isPanning)
        {
            if (e.MouseDevice.LeftButton != MouseButtonState.Pressed)
            {
                StopPanning();
            }

            var newPosition = e.GetPosition(this);
            Vector dragVector = new(newPosition.X - mousePosition.X, newPosition.Y - mousePosition.Y);
            mousePosition = newPosition;

            this.TopLeft -= dragVector;
        }
    }

    private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this, CaptureMode.SubTree);
        this.Cursor = Cursors.Hand;
        isPanning = true;
        mousePosition = e.GetPosition(this);
    }

    public Point TopLeft
    {
        get { return (Point)GetValue(TopLeftProperty); }
        set { SetValue(TopLeftProperty, value); }
    }

    public int ZoomLevel
    {
        get { return (int)GetValue(ZoomLevelProperty); }
        set { SetValue(ZoomLevelProperty, value); }
    }

    private Point BottomRight => this.TopLeft + new Vector(this.TileCanvas.ActualWidth, this.TileCanvas.ActualHeight);

    private static async void TopLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var w = (MainWindow)d;
        await w.UpdateCanvas();
    }

    public async Task UpdateCanvas()
    {
        if (this.isUpdating)
            return;
        this.isUpdating = true;

        if (TileFetcher.Key is null)
        {
            string key = System.IO.File.ReadAllText("key.txt");
            await this.TileFetcher.GetSession(key);
        }

        var tiles = this.TileCanvas.Children.OfType<TileImage>().ToList();

        var tileCount = TileFetcher.GetWidth(this.ZoomLevel);
        var worldSize = GeoMath.MapSize(this.ZoomLevel, TileFetcher.TileSize);

        var minTileX = TopLeft.X / TileFetcher.TileSize;
        minTileX = Math.Max(minTileX, 0);
        var maxTileX = BottomRight.X / TileFetcher.TileSize;
        maxTileX = Math.Min(maxTileX, tileCount - 1);
        var minTileY = TopLeft.Y / TileFetcher.TileSize;
        minTileY = Math.Max(minTileY, 0);
        var maxTileY = BottomRight.Y / TileFetcher.TileSize;
        maxTileY = Math.Min(maxTileY, tileCount - 1);

        for (int x = (int)Math.Floor(minTileX); x <= maxTileX; x++)
        {
            for (int y = (int)Math.Floor(minTileY); y <= maxTileY; y++)
            {
                TileId id = new(x, y, ZoomLevel);
                var tile = getTile(id);
                if (tile != null)
                {
                    tiles.Remove(tile);
                }
                else
                {
                    tile = createTile(id);
                    this.TileCanvas.Children.Add(tile);
                }
                if (tile != null)
                {
                    UpdateTile(tile);
                }
            }
        }

        foreach (var tile in tiles)
        {
            this.TileCanvas.Children.Remove(tile);
        }

        if (this.pins.Count == 0 && PinViewModel.DTOs.Count > 0)
        {
            foreach (var pinDto in PinViewModel.DTOs.Where(d => d.lng != null && d.lat != null))
            {
                PinViewModel vm = new(pinDto);
                this.pins.Add(vm);
            }
            this.PinsItemsControl.ItemsSource = this.pins;
        }

        foreach (var pin in this.pins)
        {
            var tl = GeoMath.GlobalPixelToPosition(
                this.TopLeft.ToVector2(),
                this.ZoomLevel,
                TileFetcher.TileSize);
            var br = GeoMath.GlobalPixelToPosition(
                this.BottomRight.ToVector2(),
                this.ZoomLevel,
                TileFetcher.TileSize);

            GeoRectangle bounds = GeoRectangle.From([tl, br])!;
            Size mapSize = new(this.TileCanvas.ActualWidth, this.TileCanvas.ActualHeight);
            Vector offset = new();
            if (this.TopLeft.X < 0)
            {
                offset.X = (float)-this.TopLeft.X;
                mapSize.Width += this.TopLeft.X;
            }
            if (this.TopLeft.Y < 0)
            {
                offset.Y = (float)-this.TopLeft.Y;
                mapSize.Height += this.TopLeft.Y;
            }
            if (this.BottomRight.X > worldSize)
            {
                mapSize.Width -= (this.BottomRight.X - worldSize);
            }
            if (this.BottomRight.Y > worldSize)
            {
                mapSize.Height -= (this.BottomRight.Y - worldSize);
            }
            pin.Update(bounds, mapSize, offset.ToVector2());
        }

        this.isUpdating = false;

        TileImage? getTile(TileId id)
        {
            return tiles.FirstOrDefault(t => t.Id is TileId i && i == id);
        }

        TileImage createTile(TileId id)
        {
            TileImage i = new()
            {
                Id = id,
            };

            if (TileFetcher.GetTileAsync(id) is string path)
            {
                i.Source = LoadFile(id, path);
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    var path = await TileFetcher.DownloadTileAsync(id);
                    if (path != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            i.Source = LoadFile(id, path);
                        });
                    }
                });
            }

            return i;
        }

        void UpdateTile(TileImage tile)
        {
            Canvas.SetLeft(tile, tile.Id.X * TileFetcher.TileSize - TopLeft.X);
            Canvas.SetTop(tile, tile.Id.Y * TileFetcher.TileSize - TopLeft.Y);
        }
    }

    private readonly Dictionary<TileId, BitmapImage> tiles = [];

    private BitmapImage LoadFile(TileId id, string path)
    {
        Uri uri = new(path);
        BitmapImage newImage = new(uri)
        {
            CacheOption = BitmapCacheOption.OnLoad,
        };
        tiles[id] = newImage;
        return newImage;
    }
}
