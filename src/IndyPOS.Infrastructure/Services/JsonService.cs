#nullable enable
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
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
    }

    public async Task SaveToFileAsync<TValue>(TValue value, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, value, _options);
    }

    public async Task<TValue> ReadFromFileAsync<TValue>(string filePath)
    {
        await using var fileStream = File.Open(filePath, FileMode.Open);
        var result = await JsonSerializer.DeserializeAsync<TValue>(fileStream, _options);

        if (result is null)
            throw new FileNotFoundException($"File ({filePath}) is not found.");

        return result;
    }

    public void SaveToFile<TValue>(TValue value, string filePath)
    {
        using var fileStream = File.Create(filePath);
        JsonSerializer.Serialize(fileStream, value, _options);
    }

    public TValue ReadFromFile<TValue>(string filePath)
    {
        using var fileStream = File.Open(filePath, FileMode.Open);
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