using System.Drawing.Printing;
using System.Globalization;
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Prints a <see cref="ReceiptDocument"/>. It no longer reads the logged-in user or the legacy
/// payment dictionary: the document carries the bill's own cashier and each payment's display name.
/// </summary>
[type:SupportedOSPlatform("windows")]
public class ReceiptPrinterService : IReceiptPrinterService
{
	private readonly ILogger<ReceiptPrinterService> _logger;
	private readonly PrintDocument _printDocument;
	private ReceiptHeader _header = new(string.Empty, string.Empty, string.Empty, string.Empty);
	private string _printerName = "XP-58";
	private ReceiptDocument? _document;

	public ReceiptPrinterService(IStoreConfigurationService storeConfigurationService,
								 ILogger<ReceiptPrinterService> logger)
	{
		_logger = logger;

		GetStoreConfiguration(storeConfigurationService);

		_printDocument = new PrintDocument();
		_printDocument.PrinterSettings.PrinterName = _printerName;
		_printDocument.PrintPage += PrintPageHandler;
	}

	private void GetStoreConfiguration(IStoreConfigurationService storeConfigurationService)
	{
		try
		{
			var config = storeConfigurationService.Get();

			_printerName = config.PrinterName ?? "XP-58";
			_header = new ReceiptHeader(config.StoreName ?? string.Empty,
										config.StoreAddressLine1 ?? string.Empty,
										config.StoreAddressLine2 ?? string.Empty,
										config.StorePhoneNumber ?? string.Empty);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to get store configuration for a receipt printer!");
			throw;
		}
	}

	public void PrintReceipt(ReceiptDocument document)
	{
		_document = document;

		_printDocument.Print();
	}

	private void PrintPageHandler(object sender, PrintPageEventArgs e)
	{
		if (_document is null || e.Graphics is null)
			throw new InvalidOperationException("There is no receipt to print.");

		ReceiptRenderer.Draw(e.Graphics, ReceiptLayout.Rows(_header, _document, CultureInfo.CurrentCulture));
	}
}
