using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InventoryProducts.Queries;

namespace IndyPOS.Windows.Forms.Interfaces
{
    public interface IInventoryController
    {
        IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id);

		Task<IEnumerable<InventoryProductDto>> GetInventoryProductsByCategoryIdAsync(int id);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        IInventoryProduct GetProductById(int id);

        void AddNewProduct(IInventoryProduct product);

        void UpdateProduct(IInventoryProduct product);

        void RemoveProductById(int id);

		int GetProductBarcodeCounter();

		void IncrementProductBarcodeCounter();
	}
}
