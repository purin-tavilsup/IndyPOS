using IndyPOS.Constants;
using IndyPOS.Sales;
using IndyPOS.Users;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace IndyPOS.Devices
{
    public class ReceiptPrinter : IReceiptPrinter
	{
		private readonly IConfig _config;
		private readonly IReadOnlyDictionary<int, string> _paymentTypeDictionary;
		private ISaleInvoice _saleInvoice;
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

		public ReceiptPrinter(IConfig config, IStoreConstants storeConstants)
		{
			_config = config;
			_paymentTypeDictionary = storeConstants.PaymentTypes;

			_brush = new SolidBrush(Color.Black);

			_logoFont = new Font(FontFamilyName, LogoFontSize);
			_textFont = new Font(FontFamilyName, TextFontSize);
			_lineFont = new Font(FontFamilyName, LineFontSize);

			_printDocument = new PrintDocument();
			_printDocument.PrintPage += PrintPageHandler;
		}

		public void PrintReceipt(ISaleInvoice saleInvoice, IUserAccount loggedInUser)
		{
			_saleInvoice = saleInvoice;
			_loggedInUser = loggedInUser;

			_printDocument.PrinterSettings.PrinterName = _config.PrinterName;
			_printDocument.Print();
		}

		private void PrintPageHandler(object sender, PrintPageEventArgs e)
		{
			var graphics = e.Graphics;
			var currentPosition = new Point(0, 5);

            PrintHeader(graphics, ref currentPosition);
			PrintInvoiceInfo(graphics, ref currentPosition);
            PrintLineItems(graphics, ref currentPosition, _saleInvoice);
            PrintPayments(graphics, ref currentPosition, _saleInvoice);
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
			PrintStoreName(graphics, _config.StoreName, position.X + 60, position.Y);

			position.Y += 30;
			
			PrintText(graphics, _config.StoreAddressLine1, position.X, position.Y);

			position.Y += SpaceOffset;
			
			PrintText(graphics, _config.StoreAddressLine2, position.X, position.Y);

			position.Y += SpaceOffset;
			
			PrintText(graphics, $"Tel.: {_config.StorePhoneNumber}", position.X, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
		}

		private void PrintInvoiceInfo(Graphics graphics, ref Point position)
        {
			position.Y += SpaceOffset;

			var invoiceNumberString = $"Invoice No.: {_saleInvoice.Id: 0000000000}";
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

		private void PrintLineItems(Graphics graphics, ref Point position, ISaleInvoice saleInvoice)
		{
			position.Y += SpaceOffset;

			PrintText(graphics, "รายการสินค้า", position.X, position.Y);
			
			position.Y += SpaceOffset / 2;

			foreach (var product in saleInvoice.Products)
			{
				position.Y += SpaceOffset;

				PrintText(graphics, product.Description, position.X, position.Y);

				position.Y += SpaceOffset;

				PrintText(graphics, $"{product.Quantity} x {product.UnitPrice:N}", position.X + 10, position.Y);

				var total = product.Quantity * product.UnitPrice;

				PrintText(graphics, $"{total:N}", position.X + PriceColumn, position.Y);
			}

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, "ราคารวม", position.X, position.Y);
			PrintText(graphics, $"{saleInvoice.InvoiceTotal:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
        }
		
		private void PrintPayments(Graphics graphics, ref Point position, ISaleInvoice saleInvoice)
        {
			position.Y += SpaceOffset;

			PrintText(graphics, "เงินที่ได้รับ", position.X, position.Y);
			
			position.Y += SpaceOffset / 2;

			foreach (var payment in saleInvoice.Payments)
			{
				position.Y += SpaceOffset;
				
				PrintText(graphics, _paymentTypeDictionary[payment.PaymentTypeId], position.X, position.Y);
				PrintText(graphics, $"{payment.Amount:N}", position.X + PriceColumn, position.Y);
			}

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, "เงินรวม", position.X, position.Y);
			PrintText(graphics, $"{saleInvoice.PaymentTotal:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintText(graphics, "เงินทอน", position.X, position.Y);
			PrintText(graphics, $"{saleInvoice.Changes:N}", position.X + PriceColumn, position.Y);

			position.Y += SpaceOffset;

			PrintLine(graphics, position.X, position.Y);
        }
	}
}
