using System.Text;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models.Report;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

public class DataFeedApiService : IDataFeedApiService
{
	private readonly HttpClient _httpClient;
	private readonly IJsonService _jsonService;
	private readonly ILogger<DataFeedApiService> _logger;
	private readonly string _baseUrl;
	private readonly bool _isDataFeedEnabled;

	public DataFeedApiService(HttpClient httpClient,
							 IConfiguration configuration,
							 IJsonService jsonService, 
							 ILogger<DataFeedApiService> logger)
	{
		_httpClient = httpClient;
		_jsonService = jsonService;
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
			var jsonString = _jsonService.Serialize(invoice);
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
			var jsonString = _jsonService.Serialize(report);
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