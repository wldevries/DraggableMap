using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace DraggableMap;

public class TileFetcher
{
    public const int TileSize = 256;

    private const string SessionPath = "https://tile.googleapis.com/v1/createSession?key=";
    private const string MapsPath = @"https://tile.googleapis.com/v1/2dtiles";

    public string? Key { get; set; }
    public string? Session { get; set; }

    public string? GetTileAsync(TileId id)
    {
        var filePath = CachePath(id);
        if (File.Exists(filePath))
        {
            return filePath;
        }

        return null;
    }

    public async Task<string> DownloadTileAsync(TileId id)
    {
        var filePath = CachePath(id);
        if (File.Exists(filePath))
        {
            return filePath;
        }

        try
        {
            await DownloadImage(id, filePath);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }

        return filePath;
    }

    private async Task DownloadImage(TileId id, string filePath)
    {
        var reqPath = $"{MapsPath}/{id.Z}/{id.X}/{id.Y}";
        QueryString reqQuery = QueryString.Empty
            .Add("key", Key)
            .Add("session", Session)
            .Add("orientation", "0");
        Uri reqUri = new(reqPath + reqQuery);
        HttpClient client = new();
        var response = await client.GetAsync(reqUri);
        response.EnsureSuccessStatusCode();

        var stream = response.Content.ReadAsStream();
        using FileStream fstream = File.OpenWrite(filePath);
        await stream.CopyToAsync(fstream);
    }

    private string CachePath(TileId id)
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "tiles", id.Z.ToString(), id.X.ToString(), id.Y + ".png");
        var dirPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(dirPath);
        return filePath;
    }

    public async Task GetSession(string key)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.PostAsJsonAsync(SessionPath + key, new SessionRequestDto());
            var responseString = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<SessionResponseDto>(responseString);
            Session = dto.session;
            Key = key;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
            throw;
        }
    }

    private record SessionRequestDto
    {
        public string mapType { get; init; } = "roadmap";
        public string language { get; init; } = "en-US";
        public string region { get; init; } = "US";
    }

    public static int GetWidth(int z)
    {
        return (int)Math.Pow(2, z);
    }
}

public class SessionResponseDto
{
    public string session { get; set; }
    public string expiry { get; set; }
    public int tileWidth { get; set; }
    public string imageFormat { get; set; }
    public int tileHeight { get; set; }
}
