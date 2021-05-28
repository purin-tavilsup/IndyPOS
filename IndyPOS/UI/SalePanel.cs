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

namespace IndyPOS
{
    public partial class SalePanel : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;

        private enum InvoiceTableColumn
        {
            Line,
            ProductCode,
            Description,
            Quantity,
            UnitPrice,
            Total,
            Delete
        }


        public SalePanel(IEventAggregator eventAggregator, IInvoicesDataService invoicesDataService)
        {
            _eventAggregator = eventAggregator;
            _invoicesDataService = invoicesDataService;

            InitializeComponent();

            InvoiceDataView.Columns.Clear();
            InvoiceDataView.ColumnCount = 6;

            InvoiceDataView.Columns[(int)InvoiceTableColumn.Line].Name = "รายการ";
            InvoiceDataView.Columns[(int)InvoiceTableColumn.Line].Width = 100;

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

            //eventAggregator.GetEvent<MessageSentEvent>().Subscribe(MessageReceived);
        }

        private void TestGetAllProductsButton_Click(object sender, EventArgs e)
        {
            var products = _invoicesDataService.GetProductsForSale().Select(x => new InventoryProductAdapter(x)).ToList();
            
            //InvoiceDataView.Rows.Clear();

            var index = 1;

            foreach(var product in products)
            {
                var barcode = product.Barcode;
                var description = product.Description;
                var quantity = 1;
                var unitPrice = product.UnitPrice;
                var total = unitPrice * quantity;

                var row = new string[] 
                {
                    index.ToString(),
                    barcode,
                    description,
                    quantity.ToString(),
                    product.UnitPrice.ToString(),
                    total.ToString()
                };
                
                InvoiceDataView.Rows.Add(row);

                index++;
            }

        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            //
        }
    }
}
