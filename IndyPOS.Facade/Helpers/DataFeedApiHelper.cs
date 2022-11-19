using IndyPOS.Common.Extensions;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text;

namespace IndyPOS.Facade.Helpers;

public class DataFeedApiHelper : IDataFeedApiHelper
{
	private readonly HttpClient _httpClient;
	private readonly IJsonUtility _jsonUtility;
	private readonly ILogger _logger;
	private readonly string _baseUrl;
	private readonly bool _isDataFeedEnabled;

	public DataFeedApiHelper(HttpClient httpClient,
							 IConfiguration configuration,
							 IJsonUtility jsonUtility, 
							 ILogger logger)
	{
		_httpClient = httpClient;
		_jsonUtility = jsonUtility;
		_logger = logger;

		_baseUrl = configuration.GetValue<string>("DataFeed:BaseUrl") ?? string.Empty;
		_isDataFeedEnabled = configuration.GetValue<bool>("DataFeed:Enabled");
		var key = configuration.GetValue<string>("DataFeed:Key") ?? string.Empty;
		
		_httpClient.DefaultRequestHeaders.Add("x-functions-key", key);
	}

	public async Task PushInvoice(Invoice invoice)
	{
		if (_isDataFeedEnabled.IsFalse()) return;

		try
		{
			var baseUri = new Uri(_baseUrl);
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
		if (_isDataFeedEnabled.IsFalse()) return;

		try
		{
			var baseUri = new Uri(_baseUrl);
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