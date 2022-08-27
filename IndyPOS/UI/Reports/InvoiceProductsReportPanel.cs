using IndyPOS.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace IndyPOS.UI.Reports
{
	[ExcludeFromCodeCoverage]
    public partial class InvoiceProductsReportPanel : UserControl
    {
		private readonly IReportController _reportController;
		private decimal _accumulatedSaleAmount;

		private enum ProductColumn
		{
			InvoiceId,
			ProductCode,
			Description,
			Quantity,
			UnitPrice,
			Total,
			Accumulation,
			DateCreated,
			Note
		}

        public InvoiceProductsReportPanel(IReportController reportController)
		{
			_reportController = reportController;

            InitializeComponent();
			InitializeInvoiceProductsDataView();
		}

		private void InitializeInvoiceProductsDataView()
		{
			#region Initialize all columns

			InvoiceProductsDataView.Columns.Clear();
			InvoiceProductsDataView.ColumnCount = 9;

			InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].Name = "Invoice ID";
			InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].Width = 200;
			InvoiceProductsDataView.Columns[(int)ProductColumn.InvoiceId].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].Name = "รหัสสินค้า";
			InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].Width = 200;
			InvoiceProductsDataView.Columns[(int)ProductColumn.ProductCode].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.Description].Name = "คำอธิบาย";
			InvoiceProductsDataView.Columns[(int)ProductColumn.Description].Width = 350;
			InvoiceProductsDataView.Columns[(int)ProductColumn.Description].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].Name = "จำนวน";
			InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].Width = 100;
			InvoiceProductsDataView.Columns[(int)ProductColumn.Quantity].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].Name = "ราคาต่อหน่วย";
			InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].Width = 150;
			InvoiceProductsDataView.Columns[(int)ProductColumn.UnitPrice].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Name = "ราคารวม";
			InvoiceProductsDataView.Columns[(int)ProductColumn.Total].Width = 150;
			InvoiceProductsDataView.Columns[(int)ProductColumn.Total].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.Accumulation].Name = "ยอดขายสะสม";
			InvoiceProductsDataView.Columns[(int)ProductColumn.Accumulation].Width = 150;
			InvoiceProductsDataView.Columns[(int)ProductColumn.Accumulation].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].Name = "วันและเวลาที่บันทึก";
			InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].Width = 200;
			InvoiceProductsDataView.Columns[(int)ProductColumn.DateCreated].ReadOnly = true;

			InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Name = "Note";
			InvoiceProductsDataView.Columns[(int)ProductColumn.Note].Width = 200;
			InvoiceProductsDataView.Columns[(int)ProductColumn.Note].ReadOnly = true;

			#endregion
		}

		private void AddProductToInvoiceDataView(IFinalInvoiceProduct product)
		{
			var columnCount = InvoiceProductsDataView.ColumnCount;
			var productRow = new object[columnCount];
			var total = product.UnitPrice * product.Quantity;

			_accumulatedSaleAmount += total;

			productRow[(int) ProductColumn.InvoiceId] = product.InvoiceId;
			productRow[(int) ProductColumn.ProductCode] = product.Barcode;
			productRow[(int) ProductColumn.Description] = product.Description;
			productRow[(int) ProductColumn.Quantity] = product.Quantity;
			productRow[(int) ProductColumn.UnitPrice] = product.UnitPrice;
			productRow[(int) ProductColumn.Total] = total;
			productRow[(int) ProductColumn.Accumulation] = _accumulatedSaleAmount;
			productRow[(int) ProductColumn.DateCreated] = product.DateCreated;
			productRow[(int) ProductColumn.Note] = product.Note;

			var rowIndex = InvoiceProductsDataView.Rows.Add(productRow);
			var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38,38,38) : Color.FromArgb(48, 48, 48);

			InvoiceProductsDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
		}

        private void InvoiceProductsReportPanel_VisibleChanged(object sender, EventArgs e)
		{
			if (!Visible)
				return;

			InvoiceProductsReport();
		}

		private void InvoiceProductsReport()
		{
			var products = _reportController.GetInvoiceProductsByDate(DateTime.Today);

			_accumulatedSaleAmount = 0m;
			InvoiceProductsDataView.Rows.Clear();

			foreach (var product in products)
			{
				AddProductToInvoiceDataView(product);
			}
		}
    }
}
