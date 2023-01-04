#nullable enable
namespace IndyPOS.Application.Interfaces;

public interface IJsonUtility
{
	Task SaveToFileAsync<TValue>(TValue value, string filePath);

	Task<TValue> ReadFromFileAsync<TValue>(string filePath);

	void SaveToFile<TValue>(TValue value, string filePath);

	TValue ReadFromFile<TValue>(string filePath);

	string Serialize<TValue>(TValue value);

	TValue? Deserialize<TValue>(string json);
}