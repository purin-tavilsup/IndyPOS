using System.Text;
using IndyPOS.Application.Interfaces;
using IndyPOS.Application.Models.Report;
using IndyPOS.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Application.Helpers;

public class DataFeedApiHelper : IDataFeedApiHelper
{
	private readonly HttpClient _httpClient;
	private readonly IJsonUtility _jsonUtility;
	private readonly ILogger<DataFeedApiHelper> _logger;
	private readonly string _baseUrl;
	private readonly bool _isDataFeedEnabled;

	public DataFeedApiHelper(HttpClient httpClient,
							 IConfiguration configuration,
							 IJsonUtility jsonUtility, 
							 ILogger<DataFeedApiHelper> logger)
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
			_logger.LogWarning(ex, $"Failed to push Invoice ({invoice.Id}) to DataFeed.");
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
			_logger.LogWarning(ex, $"Failed to push SalesReport ({report.Id}) to DataFeed.");
		}
	}
}