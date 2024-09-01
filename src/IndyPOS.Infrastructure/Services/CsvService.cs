using CsvHelper;
using CsvHelper.Configuration;
using IndyPOS.Application.Common.Interfaces;
using System.Globalization;
using System.Text;

namespace IndyPOS.Infrastructure.Services;

public class CsvService : ICsvService
{
	private readonly Encoding _fileEncoding = Encoding.UTF8;

	public async Task WriteToCsvFile<T>(IEnumerable<T> records, string filePath)
	{
		if (File.Exists(filePath))
		{
			await AppendToExistingFileAsync(records,  filePath);
			return;
		}

		await WriteToNewFileAsync(records, filePath);
	}

	private async Task WriteToNewFileAsync<T>(IEnumerable<T> records, string filePath)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ","
		};

		await using var stream = File.Open(filePath, FileMode.Create);
		await using var writer = new StreamWriter(stream, _fileEncoding);
		await using var csv = new CsvWriter(writer, config);

		await csv.WriteRecordsAsync(records);
	}

	private async Task AppendToExistingFileAsync<T>(IEnumerable<T> records, string filePath)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			// Do not write the header again
			HasHeaderRecord = false,
			Delimiter = ","
		};

		await using var stream = File.Open(filePath, FileMode.Append);
		await using var writer = new StreamWriter(stream, _fileEncoding);
		await using var csv = new CsvWriter(writer, config);

		await csv.WriteRecordsAsync(records);
	}
}