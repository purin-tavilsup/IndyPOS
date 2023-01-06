using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInventoryProductRepository
{
	InventoryProduct? GetProductByBarcode(string barcode);

	IEnumerable<InventoryProduct> GetProductsByCategoryId(int id);

	InventoryProduct? GetProductById(int id);

	int AddProduct(InventoryProduct product);

	bool UpdateProduct(InventoryProduct product);

	bool UpdateProductQuantityById(int id, int quantity);

	bool RemoveProduct(InventoryProduct product);

	bool RemoveProductById(int id);

	int GetProductBarcodeCounter();

	bool UpdateProductBarcodeCounter(int counter);
}