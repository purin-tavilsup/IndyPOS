namespace IndyPOS.Application.Common.Interfaces;

public interface ICsvService
{
	Task WriteToCsvFile<T>(IEnumerable<T> records, string filePath);
}