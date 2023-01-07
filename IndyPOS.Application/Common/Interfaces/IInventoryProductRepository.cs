using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInventoryProductRepository
{
	int Add(InventoryProduct product);

	InventoryProduct? GetById(int id);

	bool Update(InventoryProduct product);

	bool RemoveById(int id);

	bool Remove(InventoryProduct product);

	InventoryProduct? GetByBarcode(string barcode);

	IEnumerable<InventoryProduct> GetProductsByCategoryId(int id);

	bool UpdateProductQuantityById(int id, int quantity);

	int GetProductBarcodeCounter();

	bool UpdateProductBarcodeCounter(int counter);
}