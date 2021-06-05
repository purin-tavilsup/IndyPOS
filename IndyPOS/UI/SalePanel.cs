using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Prism.Events;
using IndyPOS.DataServices;
using IndyPOS.Adapters;
using IndyPOS.Controllers;
using IndyPOS.EventAggregator;
using IndyPOS.Extensions;

namespace IndyPOS
{
    public partial class SalePanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;
        private SaleInvoiceController _saleInvoiceController;

        private enum InvoiceTableColumn
        {
            ProductCode,
            Description,
            Quantity,
            UnitPrice,
            Total
        }

        public SalePanel(IEventAggregator eventAggregator, IInvoicesDataService invoicesDataService, IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _invoicesDataService = invoicesDataService;
            _inventoryProductsDataService = inventoryProductsDataService;

            InitializeComponent();

            _saleInvoiceController = new SaleInvoiceController(_eventAggregator, _invoicesDataService, _inventoryProductsDataService);
            

            InvoiceDataView.Columns.Clear();
            InvoiceDataView.ColumnCount = 5;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.ProductCode].Name = "รหัสสินค้า";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.ProductCode].Width = 200;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.Description].Name = "คำอธิบาย";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.Description].Width = 500;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.Quantity].Name = "จำนวน";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.Quantity].Width = 100;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.UnitPrice].Name = "ราคาต่อหน่วย";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.UnitPrice].Width = 170;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.Total].Name = "ราคารวม";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.Total].Width = 150;

            eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Subscribe(SaleInvoiceProductChanged);
            eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Subscribe(SaleInvoiceProductChanged);
        }

        private void TestGetAllProductsButton_Click(object sender, EventArgs e)
        {
            //
            
            var selectedCell = InvoiceDataView.SelectedCells[0];
            var rowIndex = selectedCell.RowIndex;
            var columnIndex = selectedCell.ColumnIndex;

            var selectedRow = InvoiceDataView.Rows[rowIndex];
            var barcode = selectedRow.Cells[(int)InvoiceTableColumn.ProductCode].Value;

        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            _saleInvoiceController.AddProductToSaleInvoice("8850999009674");
        }

        private void RemoveProductButton_Click(object sender, EventArgs e)
        {
            var barcode = GetProductBarcodeFromSelectedProduct();

            _saleInvoiceController.RemoveProductFromSaleInvoice(barcode);
        }

        private string GetProductBarcodeFromSelectedProduct()
        {
            if (InvoiceDataView.SelectedCells.Count == 0)
                return string.Empty;

            var selectedCell = InvoiceDataView.SelectedCells[0];
            var rowIndex = selectedCell.RowIndex;
            var selectedRow = InvoiceDataView.Rows[rowIndex];
            var barcode = selectedRow.Cells[(int)InvoiceTableColumn.ProductCode].Value as string;

            return barcode;
        }

        private void SaleInvoiceProductChanged(string barcode)
        {
            InvoiceDataView.UIThread(delegate
            {
                InvoiceDataView.Rows.Clear();

                var products = _saleInvoiceController.Products;

                foreach (var product in products)
                {
                    AddProductToInvoiceDataView(product);
                }
            });
        }

        private void AddProductToInvoiceDataView(ISaleInvoiceProduct product)
        {
            var columnCount = InvoiceDataView.ColumnCount;
            var index = InvoiceDataView.Rows.Count + 1;
            var productRow = new string[columnCount];
            var total = product.UnitPrice * product.Quantity;

            productRow[(int)InvoiceTableColumn.ProductCode] = product.Barcode;
            productRow[(int)InvoiceTableColumn.Description] = product.Description;
            productRow[(int)InvoiceTableColumn.Quantity] = product.Quantity.ToString();
            productRow[(int)InvoiceTableColumn.UnitPrice] = product.UnitPrice.ToString();
            productRow[(int)InvoiceTableColumn.Total] = total.ToString();

            InvoiceDataView.Rows.Add(productRow);
        }

        private void InvoiceDataView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != (int)InvoiceTableColumn.Quantity)
                return;

            var selectedRow = ((DataGridView)sender).Rows[e.RowIndex];
            var barcode = selectedRow.Cells[(int)InvoiceTableColumn.ProductCode].Value;
        }
       
    }
}
