using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DraggableMap;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int MinZoom = 0;
    private const int MaxZoom = 14;

    public static readonly DependencyProperty TopLeftProperty =
        DependencyProperty.Register("TopLeft", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0, 0), TopLeftChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register("ZoomLevel", typeof(int), typeof(MainWindow), new PropertyMetadata(3));

    private bool isPanning = false;
    private Point mousePosition = new();
    private TileFetcher TileFetcher = new();
    private bool isUpdating = false;

    public MainWindow()
    {
        InitializeComponent();

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
                    tile = await createTile(id);
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

        this.isUpdating = false;

        TileImage? getTile(TileId id)
        {
            return tiles.FirstOrDefault(t => t.Id is TileId i && i == id);
        }

        async Task<TileImage> createTile(TileId id)
        {
            return new TileImage()
            {
                Id = id,
                Source = await TileFetcher.GetTileAsync(id),
            };
        }

        void UpdateTile(TileImage tile)
        {
            Canvas.SetLeft(tile, tile.Id.X * TileFetcher.TileSize - TopLeft.X);
            Canvas.SetTop(tile, tile.Id.Y * TileFetcher.TileSize - TopLeft.Y);
        }
    }
}
