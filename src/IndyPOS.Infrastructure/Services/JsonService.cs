using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace IndyPOS.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public class JsonService : IJsonService
{
    private readonly JsonSerializerOptions _options;

    public JsonService()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };
    }

    public async Task SaveToFileAsync<TValue>(TValue value, string filePath)
    {
        EnsureDirectoryExists(filePath);
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, value, _options);
    }

    public async Task<TValue> ReadFromFileAsync<TValue>(string filePath)
    {
        // Open read-only: the file may be owned by an elevated installer and
        // grant standard users read-only access. The 2-arg File.Open overload
        // requests ReadWrite, which would fail with UnauthorizedAccessException.
        await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = await JsonSerializer.DeserializeAsync<TValue>(fileStream, _options);

        if (result is null)
            throw new FileNotFoundException($"File ({filePath}) is not found.");

        return result;
    }

    public void SaveToFile<TValue>(TValue value, string filePath)
    {
        EnsureDirectoryExists(filePath);
        using var fileStream = File.Create(filePath);
        JsonSerializer.Serialize(fileStream, value, _options);
    }

    // Guards against DirectoryNotFoundException on a fresh machine where the
    // target folder (e.g. the versioned Config dir) doesn't exist yet.
    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public TValue ReadFromFile<TValue>(string filePath)
    {
        // Open read-only — see ReadFromFileAsync for why ReadWrite (the 2-arg
        // File.Open default) breaks on an installer-owned, read-only-for-users file.
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.Deserialize<TValue>(fileStream, _options);

        if (result is null)
            throw new FileNotFoundException($"File ({filePath}) is not found.");

        return result;
    }

    public string Serialize<TValue>(TValue value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public TValue? Deserialize<TValue>(string json)
    {
        return JsonSerializer.Deserialize<TValue>(json, _options);
    }
}