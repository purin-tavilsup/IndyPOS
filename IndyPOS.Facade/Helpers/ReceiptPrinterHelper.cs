using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace IndyPOS.Facade.Helpers
{
	public class ReceiptPrinterHelper : IReceiptPrinterHelper
	{
		private readonly IConfiguration _configuration;
		private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
		private IInvoiceInfo _invoiceInfo;
		private IUserAccount _loggedInUser;
        private readonly PrintDocument _printDocument;
		private readonly SolidBrush _brush;
		private readonly Font _textFont;
		private readonly Font _lineFont;
		private readonly Font _logoFont;

		private const int SpaceOffset = 11;
		private const int LogoFontSize = 15;
		private const int TextFontSize = 7;
		private const int LineFontSize = 5;
		private const int PriceColumn = 135;

		private const string FontFamilyName = "FC Subject [Non-commercial] Reg";
		private const string LineString = "-------------------------------------------------------";

		public ReceiptPrinterHelper(IConfiguration configuration, IStoreConstants storeConstants)
		{
			_configuration = configuration;
			_paymentTypeDictionary = storeConstants.PaymentTypes;

			_brush = new SolidBrush(Color.Black);

			_logoFont = new Font(FontFamilyName, LogoFontSize);
			_textFont = new Font(FontFamilyName, TextFontSize);
			_lineFont = new Font(FontFamilyName, LineFontSize);

			_printDocument = new PrintDocument();
			_printDocument.PrintPage += PrintPageHandler;
		}

		public void PrintReceipt(IInvoiceInfo invoiceInfo, IUserAccount loggedInUser)
		{
			_invoiceInfo = invoiceInfo;
			_loggedInUser = loggedInUser;

			_printDocument.PrinterSettings.PrinterName = _configuration.PrinterName;
			_printDocument.Print();
		}

		private void PrintPageHandler(object sender, PrintPageEventArgs e)
		{
			var graphics = e.Graphics;
			var currentPosition = new Point(0, 5);

            PrintHeader(graphics, ref currentPosition);
			PrintInvoiceInfo(graphics, ref currentPosition);
            PrintLineItems(graphics, ref currentPosition);
            PrintPayments(graphics, ref currentPosition);
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
			PrintStoreName(graphics, _configuration.StoreName, position.X + 60, position.Y);

			position.Y += 30;
			
			PrintText(graphics, _configuration.StoreAddressLine1, position.X, position.Y);

			position.Y += SpaceOffset;
			
			PrintText(graphics, _configuration.StoreAddressLine2, position.X, position.Y);

			position.Y += SpaceOffset;
			
			PrintText(graphics, $"Tel.: {_configuration.StorePhoneNumber}", position.X, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
		}

		private void PrintInvoiceInfo(Graphics graphics, ref Point position)
        {
			position.Y += SpaceOffset;

			var invoiceNumberString = $"Invoice No.: {_invoiceInfo.Id: 0000000000}";
			PrintText(graphics, invoiceNumberString, position.X, position.Y);

			position.Y += SpaceOffset;

			var dateString = $"Date: {DateTime.Now:dd MMMM yyyy}";
			PrintText(graphics, dateString, position.X, position.Y);

			position.Y += SpaceOffset;

			var timeString = $"Time: {DateTime.Now:hh:mm tt}";
			PrintText(graphics, timeString, position.X, position.Y);

			position.Y += SpaceOffset;

			var cashierName = $"Cashier: {_loggedInUser.FirstName} {_loggedInUser.LastName}";
			PrintText(graphics, cashierName, position.X, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
        }

		private void PrintLineItems(Graphics graphics, ref Point position)
		{
			position.Y += SpaceOffset;

			PrintText(graphics, "รายการสินค้า", position.X, position.Y);
			
			position.Y += SpaceOffset / 2;

			foreach (var product in _invoiceInfo.Products)
			{
				position.Y += SpaceOffset;

				var description = GetProductDescription(product);

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
			PrintText(graphics, $"{_invoiceInfo.InvoiceTotal:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
        }

		private string GetProductDescription(ISaleInvoiceProduct product)
		{
			if (_invoiceInfo.IsRefundInvoice && product.Note.HasValue())
				return $"{product.Description} : {product.Note} (คืนสินค้า)"; 

			if (_invoiceInfo.IsRefundInvoice)
				return $"{product.Description} : (คืนสินค้า)";

			if (product.Note.HasValue())
				return $"{product.Description} : {product.Note}";

			return product.Description;
		}
		
		private void PrintPayments(Graphics graphics, ref Point position)
        {
			position.Y += SpaceOffset;

			PrintText(graphics, "รายการเงิน", position.X, position.Y);
			
			position.Y += SpaceOffset / 2;

			foreach (var payment in _invoiceInfo.Payments)
			{
				position.Y += SpaceOffset;
				
				var description = GetPaymentDescription(payment);

				PrintText(graphics, description, position.X, position.Y);
				PrintText(graphics, $"{payment.Amount:N}", position.X + PriceColumn, position.Y);
			}

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, "เงินรวม", position.X, position.Y);
			PrintText(graphics, $"{_invoiceInfo.PaymentTotal:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, "เงินทอน", position.X, position.Y);
			PrintText(graphics, $"{_invoiceInfo.Changes:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
        }

		private string GetPaymentDescription(IPayment payment)
		{
			var paymentType = _paymentTypeDictionary[payment.PaymentTypeId];

			if (_invoiceInfo.IsRefundInvoice)
				return $"{paymentType} : คืนเงิน";

			if (payment.Note.HasValue())
				return $"{paymentType} : {payment.Note}";

			return paymentType;
		}
	}
}
