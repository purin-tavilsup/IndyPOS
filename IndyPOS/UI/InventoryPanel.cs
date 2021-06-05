using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IndyPOS
{
    public partial class InventoryPanel : UserControl
    {
        public InventoryPanel()
        {
            InitializeComponent();
            InitializeProductCategories();
        }

        private void InitializeProductCategories()
        {
            var categories = Machine.StoreConstants.ProductCategories;

            ProductCategoryListBox.Items.Clear();
            
            foreach (var item in categories)
            {
                ProductCategoryListBox.Items.Add(item.Value);
            }
        }

        private void GetProductsByCategoryButton_Click(object sender, EventArgs e)
        {
            //
        }

        private void SearchProductByBarcodeButton_Click(object sender, EventArgs e)
        {
            //
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            //
        }

        private void RemoveProductButton_Click(object sender, EventArgs e)
        {
            //
        }
    }
}
