using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces
{
    public interface IInventoryProductRepository
    {
        InventoryProduct GetProductByBarcode(string barcode);

        IList<InventoryProduct> GetProductsByCategoryId(int id);

        InventoryProduct GetProductById(int id);

        int AddProduct(InventoryProduct product);

        void UpdateProduct(InventoryProduct product);

		void UpdateProductQuantityById(int id, int quantity);

        void RemoveProduct(InventoryProduct product);

        void RemoveProductById(int id);

		int GetProductBarcodeCounter();

		void UpdateProductBarcodeCounter(int counter);
	}
}
