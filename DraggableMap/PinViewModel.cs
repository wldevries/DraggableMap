using ReactiveUI;
using System.Numerics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace DraggableMap;

public class PinViewModel : ReactiveObject
{
    private Matrix transformation;

    public PinViewModel(PinDto dto)
    {
        this.DTO = dto;
    }

    public static IReadOnlyCollection<PinDto> DTOs { get; private set; } = [];

    public PinDto DTO { get; init; }

    public string Name => this.DTO.nameEN ?? this.DTO.nameNL ?? this.DTO.description ?? this.DTO.id;

    public double Width { get; } = 40;
    public double Height { get; } = 40;

    public Matrix Transformation
    {
        get => transformation;
        set => this.RaiseAndSetIfChanged(ref transformation, value);
    }

    public void Update(
        GeoRectangle bounds,
        Size resolution,
        Vector2 offset)
    {
        if (this.DTO.lng is double lng && this.DTO.lat is double lat)
        {
            GeoCoordinate location = new(lat, lng);
            var position = GeoMath.ProjectToMap(location, bounds, resolution.Width, resolution.Height);
            Matrix t = new();
            t.Translate(
                position.X - this.Width / 2.0 + offset.X,
                position.Y - this.Height + this.Height * 0.15 + offset.Y);
            this.Transformation = t;
        }
    }

    public static void Load()
    {
        var json = System.IO.File.ReadAllText("pins.json");
        DTOs = JsonSerializer.Deserialize<IReadOnlyCollection<PinDto>>(json) ?? [];
    }

}
