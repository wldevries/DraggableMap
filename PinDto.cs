namespace DraggableMap;

public record PinDto
{
    public string id { get; init; }
    public string description { get; init; }
    public string nameNL { get; init; }
    public string nameEN { get; init; }
    public string nameFR { get; init; }
    public string nameDE { get; init; }
    public string country { get; init; }
    public double? lng { get; init; }
    public double? lat { get; init; }
    public string region { get; init; }
}
