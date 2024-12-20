﻿using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using Microsoft.Extensions.Logging;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace IndyPOS.Infrastructure.Services;

[type:SupportedOSPlatform("windows")]
public class ReceiptPrinterService : IReceiptPrinterService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly ILogger<ReceiptPrinterService> _logger;
	private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
	private IInvoiceInfo? _invoiceInfo;
	private ILoggedInUser? _loggedInUser;
	private readonly PrintDocument _printDocument;
	private readonly SolidBrush _brush;
	private readonly Font _textFont;
	private readonly Font _lineFont;
	private readonly Font _logoFont;
	private string _printerName = string.Empty;
	private string _storeName = string.Empty;
	private string _storeAddressLine1 = string.Empty;
	private string _storeAddressLine2 = string.Empty;
	private string _storePhoneNumber = string.Empty;

	private const int SpaceOffset = 11;
	private const int LogoFontSize = 15;
	private const int TextFontSize = 7;
	private const int LineFontSize = 5;
	private const int PriceColumn = 135;

	private const string FontFamilyName = "FC Subject [Non-commercial] Reg";
	private const string LineString = "-------------------------------------------------------";

	public ReceiptPrinterService(IStoreConfigurationService storeConfigurationService, 
								 IStoreConstants storeConstants,
								 IEventAggregator eventAggregator,
								 ILogger<ReceiptPrinterService> logger)
	{
		_logger = logger;
		_eventAggregator = eventAggregator;
		_paymentTypeDictionary = storeConstants.PaymentTypes;

		GetStoreConfiguration(storeConfigurationService);

		_brush = new SolidBrush(Color.Black);

		_logoFont = new Font(FontFamilyName, LogoFontSize);
		_textFont = new Font(FontFamilyName, TextFontSize);
		_lineFont = new Font(FontFamilyName, LineFontSize);

		_printDocument = new PrintDocument();
		_printDocument.PrinterSettings.PrinterName = _printerName;
		_printDocument.PrintPage += PrintPageHandler;

		SubscribeEvents();
	}

	private void SubscribeEvents()
	{
		_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
		_eventAggregator.GetEvent<UserLoggedOutEvent>().Subscribe(OnUserLoggedOut);
	}

	private void OnUserLoggedIn(ILoggedInUser loggedInUser)
	{
		_loggedInUser = loggedInUser;
	}

	private void OnUserLoggedOut()
	{
		_loggedInUser = null;
	}

	private void GetStoreConfiguration(IStoreConfigurationService storeConfigurationService)
	{
		try
		{
			var config = storeConfigurationService.Get();

			_printerName = config.PrinterName ?? "XP-58";
			_storeName = config.StoreName ?? string.Empty;
			_storeAddressLine1 = config.StoreAddressLine1 ?? string.Empty;
			_storeAddressLine2 = config.StoreAddressLine2 ?? string.Empty;
			_storePhoneNumber = config.StorePhoneNumber ?? string.Empty;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to get store configuration for a receipt printer!");
			throw;
		}
	}

	public void PrintReceipt(IInvoiceInfo invoiceInfo)
	{
		_invoiceInfo = invoiceInfo;

		_printDocument.Print();
	}
	
	private void PrintPageHandler(object sender, PrintPageEventArgs e)
	{
		if (_invoiceInfo is null || _loggedInUser is null || e.Graphics is null)
			throw new ArgumentNullException("", "At least one of parameters required for printing a receipt is null.");
		
		var graphics = e.Graphics;
		var currentPosition = new Point(0, 5);

		PrintHeader(graphics, ref currentPosition);
		PrintInvoiceInfo(_invoiceInfo, _loggedInUser, graphics, ref currentPosition);
		PrintLineItems(_invoiceInfo, graphics, ref currentPosition);
		PrintPayments(_invoiceInfo, graphics, ref currentPosition);
	}

	private void PrintStoreName(Graphics graphics, string text, int x, int y)
	{
		graphics.DrawString(text, _logoFont, _brush, x, y);
	}

	private void PrintText(Graphics graphics, string text, int x, int y)
	{
		graphics.DrawString(text, _textFont, _brush, x, y);
	}

	private void PrintLine(Graphics graphics, int x, int y)
	{
		graphics.DrawString(LineString, _lineFont, _brush, x, y);
	}

	private void PrintHeader(Graphics graphics, ref Point position)
	{
		PrintStoreName(graphics, _storeName, position.X + 60, position.Y);

		position.Y += 30;
			
		PrintText(graphics, _storeAddressLine1, position.X, position.Y);

		position.Y += SpaceOffset;
			
		PrintText(graphics, _storeAddressLine2, position.X, position.Y);

		position.Y += SpaceOffset;
			
		PrintText(graphics, $"Tel.: {_storePhoneNumber}", position.X, position.Y);

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);
	}

	private void PrintInvoiceInfo(IInvoiceInfo invoiceInfo, ILoggedInUser loggedInUser, Graphics graphics, ref Point position)
	{
		position.Y += SpaceOffset;

		var invoiceNumberString = $"Invoice No.: {invoiceInfo.Id: 0000000000}";
		PrintText(graphics, invoiceNumberString, position.X, position.Y);

		position.Y += SpaceOffset;

		var dateString = $"Date: {DateTime.Now:dd MMMM yyyy}";
		PrintText(graphics, dateString, position.X, position.Y);

		position.Y += SpaceOffset;

		var timeString = $"Time: {DateTime.Now:hh:mm tt}";
		PrintText(graphics, timeString, position.X, position.Y);

		position.Y += SpaceOffset;

		var cashierName = $"Cashier: {loggedInUser.FirstName} {loggedInUser.LastName}";
		PrintText(graphics, cashierName, position.X, position.Y);

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);
	}

	private void PrintLineItems(IInvoiceInfo invoiceInfo, Graphics graphics, ref Point position)
	{
		position.Y += SpaceOffset;

		PrintText(graphics, "รายการสินค้า", position.X, position.Y);
			
		position.Y += SpaceOffset / 2;

		foreach (var product in invoiceInfo.Products)
		{
			position.Y += SpaceOffset;

			var description = GetProductDescription(invoiceInfo, product);

			PrintText(graphics, description, position.X, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, $"{product.Quantity} x {product.UnitPrice:N}", position.X + 10, position.Y);

			var total = product.Quantity * product.UnitPrice;

			PrintText(graphics, $"{total:N}", position.X + PriceColumn, position.Y);
		}

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);

		position.Y += SpaceOffset;

		PrintText(graphics, "ราคารวม", position.X, position.Y);
		PrintText(graphics, $"{invoiceInfo.InvoiceTotal:N}", position.X + PriceColumn, position.Y);

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);
	}

	private static string GetProductDescription(IInvoiceInfo invoiceInfo, Product product)
	{
		if (invoiceInfo.IsRefundInvoice && product.Note.HasValue())
			return $"{product.Description} : {product.Note} (คืนสินค้า)"; 

		if (invoiceInfo.IsRefundInvoice)
			return $"{product.Description} : (คืนสินค้า)";

		if (product.Note.HasValue())
			return $"{product.Description} : {product.Note}";

		return product.Description;
	}
		
	private void PrintPayments(IInvoiceInfo invoiceInfo, Graphics graphics, ref Point position)
	{
		position.Y += SpaceOffset;

		PrintText(graphics, "รายการเงิน", position.X, position.Y);
			
		position.Y += SpaceOffset / 2;

		foreach (var payment in invoiceInfo.Payments)
		{
			position.Y += SpaceOffset;
				
			var description = GetPaymentDescription(invoiceInfo, payment);

			PrintText(graphics, description, position.X, position.Y);
			PrintText(graphics, $"{payment.Amount:N}", position.X + PriceColumn, position.Y);
		}

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);

		position.Y += SpaceOffset;

		PrintText(graphics, "เงินรวม", position.X, position.Y);
		PrintText(graphics, $"{invoiceInfo.PaymentTotal:N}", position.X + PriceColumn, position.Y);

		position.Y += SpaceOffset;

		PrintText(graphics, "เงินทอน", position.X, position.Y);
		PrintText(graphics, $"{invoiceInfo.Changes:N}", position.X + PriceColumn, position.Y);

		position.Y += SpaceOffset;

		PrintLine(graphics, position.X, position.Y);
	}

	private string GetPaymentDescription(IInvoiceInfo invoiceInfo, Payment payment)
	{
		var paymentType = _paymentTypeDictionary[payment.PaymentTypeId];

		if (invoiceInfo.IsRefundInvoice)
			return $"{paymentType} : คืนเงิน";

		if (payment.Note.HasValue())
			return $"{paymentType} : {payment.Note}";

		return paymentType;
	}
}