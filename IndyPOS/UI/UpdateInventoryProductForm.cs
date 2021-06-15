using IndyPOS.Constants;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using IndyPOS.Controllers;
using IndyPOS.Inventory;
using IndyPOS.Adapters;

namespace IndyPOS.UI
{
    public partial class UpdateInventoryProductForm : Form
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IStoreConstants _storeConstants;
        private readonly IInventoryController _inventoryController;
        private IReadOnlyDictionary<int, string> _productCategoryDictionary;

        public UpdateInventoryProductForm(IEventAggregator eventAggregator, 
            IStoreConstants storeConstants, 
            IInventoryController inventoryController)
        {
            _eventAggregator = eventAggregator;
            _storeConstants = storeConstants;
            _inventoryController = inventoryController;
            _productCategoryDictionary = _storeConstants.ProductCategories;

            InitializeComponent();
            InitializeProductCategories();
        }

        public void ShowDialog(IInventoryProduct product)
        {
            ProductCodeTextBox.Text = product.Barcode;
            ProductCodeTextBox.ReadOnly = true;

            PopulateProductProperties(product);
            ShowDialog();
        }

        private void PopulateProductProperties(IInventoryProduct product)
        {
            DescriptionTextBox.Text = product.Description;
            QuantityTextBox.Text = product.QuantityInStock.ToString();
            UnitPriceTextBox.Text = product.UnitPrice.ToString("0.00");
            UnitCostTextBox.Text = product.UnitCost.HasValue ? product.UnitCost.Value.ToString("0.00") : string.Empty;
            CategoryComboBox.Text = _productCategoryDictionary[product.Category];
            ManufacturerTextBox.Text = product.Manufacturer;
            BrandTextBox.Text = product.Brand;
            CommentTextBox.Text = product.Comment;
        }

        private bool ValidateProductEntry()
        {
            if (string.IsNullOrWhiteSpace(ProductCodeTextBox.Text))
            {
                MessageBox.Show("กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง", "รหัสสินค้าไม่ถูกต้อง");
                return false;
            }
                
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                return false;
            }

            if(int.TryParse(QuantityTextBox.Text.Trim(), out var quantity))
            {
                if (quantity < 1)
                {
                    MessageBox.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
                return false;
            }

            if (decimal.TryParse(UnitPriceTextBox.Text.Trim(), out var unitPrice))
            {
                if (unitPrice < 0m)
                {
                    MessageBox.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                return false;
            }

            if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Text.Trim()))
            {
                MessageBox.Show("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
                return false;
            }

            return true;
        }

        private void InitializeProductCategories()
        {
            CategoryComboBox.Items.Clear();

            foreach (var item in _productCategoryDictionary)
            {
                CategoryComboBox.Items.Add(item.Value);
            }
        }

        private void SaveProductEntryButton_Click(object sender, EventArgs e)
        {
            if (!ValidateProductEntry())
                return;

            var product = CreateNewProduct();

            _inventoryController.AddNewProduct(product);

            Close();
        }

        private IInventoryProduct CreateNewProduct()
        {
            var quantity = int.Parse(QuantityTextBox.Text.Trim());
            var unitPrice = decimal.Parse(UnitPriceTextBox.Text.Trim());
            var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Text);
            var categoryId = category.Key;

            var product = new InventoryProduct
            {
                Barcode = ProductCodeTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                QuantityInStock = quantity,
                UnitPrice = unitPrice,
                Category = categoryId
            };

            if (decimal.TryParse(UnitCostTextBox.Text.Trim(), out var unitCost))
                product.UnitCost = unitCost;

            if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Text))
                product.Manufacturer = ManufacturerTextBox.Text;

            if (!string.IsNullOrWhiteSpace(BrandTextBox.Text))
                product.Brand = BrandTextBox.Text;

            if (!string.IsNullOrWhiteSpace(CommentTextBox.Text))
                product.Comment = CommentTextBox.Text;

            return product;
        }

        private void CancelProductEntryButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
