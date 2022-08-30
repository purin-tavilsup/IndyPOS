using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Helpers
{
	public class DataFeedApiHelper : IDataFeedApiHelper
    {
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger;

        public DataFeedApiHelper(HttpClient httpClient,
								 IConfiguration configuration,
								 IJsonUtility jsonUtility, 
								 ILogger logger)
        {
			_httpClient = httpClient;
			_configuration = configuration;
			_jsonUtility = jsonUtility;
			_logger = logger;

			ConfigureHeaders();
		}

		private void ConfigureHeaders()
		{
			_httpClient.DefaultRequestHeaders.Add("x-functions-key",_configuration.DataFeedKey);
		}

        public async Task PushInvoice(Invoice invoice)
		{
			if (_configuration.DataFeedEnabled.IsFalse()) return;

			try
			{
				var baseUri = new Uri(_configuration.DataFeedBaseUri);
				var uri = new Uri(baseUri, "invoices");
				var jsonString = _jsonUtility.Serialize(invoice);
				var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync(uri, content);

				response.EnsureSuccessStatusCode();
			}
			catch (Exception ex)
			{
				_logger.Error(ex, $"Failed to push Invoice ({invoice.Id}) to DataFeed.");
				throw;
			}
		}

        public async Task PushReport(SalesReport report)
        {
			if (_configuration.DataFeedEnabled.IsFalse()) return;

			try
			{
				var baseUri = new Uri(_configuration.DataFeedBaseUri);
				var uri = new Uri(baseUri, "salesreports");
				var jsonString = _jsonUtility.Serialize(report);
				var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync(uri, content);

				response.EnsureSuccessStatusCode();
			}
			catch (Exception ex)
			{
				_logger.Error(ex, $"Failed to push SalesReport ({report.Id}) to DataFeed.");
				throw;
			}
        }
	}
}
