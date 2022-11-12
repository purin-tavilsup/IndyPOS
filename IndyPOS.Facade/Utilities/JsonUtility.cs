using IndyPOS.Facade.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace IndyPOS.Facade.Utilities
{
	[ExcludeFromCodeCoverage]
	public class JsonUtility : IJsonUtility
    {
		private readonly JsonSerializerOptions _options;

        public JsonUtility()
        {
			_options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
        }

        public async Task SaveToFileAsync<TValue>(TValue value, string filePath)
        {
			using (var fileStream = File.Create(filePath))
			{
				await JsonSerializer.SerializeAsync(fileStream, value, _options);
			}
        }

        public async Task<TValue> ReadFromFileAsync<TValue>(string filePath)
        {
			using (var fileStream = File.Open(filePath, FileMode.Open))
			{
				return await JsonSerializer.DeserializeAsync<TValue>(fileStream, _options);
			}
        }

		public void SaveToFile<TValue>(TValue value, string filePath)
		{
			using (var fileStream = File.Create(filePath))
			{
				JsonSerializer.Serialize(fileStream, value, _options);
			}
		}

		public TValue ReadFromFile<TValue>(string filePath)
		{
			using (var fileStream = File.Open(filePath, FileMode.Open))
			{
				return JsonSerializer.Deserialize<TValue>(fileStream, _options);
			}
		}

		public string Serialize<TValue>(TValue value)
		{
			return JsonSerializer.Serialize(value, _options);
		}

		public TValue Deserialize<TValue>(string json)
		{
			return JsonSerializer.Deserialize<TValue>(json, _options);
		}
    }
}
