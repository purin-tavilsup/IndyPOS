using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using Serilog;
using System.Text;

namespace IndyPOS.Facade.Helpers;

public class DataFeedApiHelper : IDataFeedApiHelper
{
	private readonly HttpClient _httpClient;
	private readonly IConfig _config;
	private readonly IJsonUtility _jsonUtility;
	private readonly ILogger _logger;

	public DataFeedApiHelper(HttpClient httpClient,
							 IConfig config,
							 IJsonUtility jsonUtility, 
							 ILogger logger)
	{
		_httpClient = httpClient;
		_config = config;
		_jsonUtility = jsonUtility;
		_logger = logger;

		ConfigureHeaders();
	}

	private void ConfigureHeaders()
	{
		_httpClient.DefaultRequestHeaders.Add("x-functions-key",_config.DataFeedKey);
	}

	public async Task PushInvoice(Invoice invoice)
	{
		if (_config.DataFeedEnabled.IsFalse()) return;

		try
		{
			var baseUri = new Uri(_config.DataFeedBaseUri);
			var uri = new Uri(baseUri, "invoices");
			var jsonString = _jsonUtility.Serialize(invoice);
			var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(uri, content);

			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.Error(ex, $"Failed to push Invoice ({invoice.Id}) to DataFeed.");
		}
	}

	public async Task PushReport(SalesReport report)
	{
		if (_config.DataFeedEnabled.IsFalse()) return;

		try
		{
			var baseUri = new Uri(_config.DataFeedBaseUri);
			var uri = new Uri(baseUri, "salesreports");
			var jsonString = _jsonUtility.Serialize(report);
			var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(uri, content);

			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.Error(ex, $"Failed to push SalesReport ({report.Id}) to DataFeed.");
		}
	}
}