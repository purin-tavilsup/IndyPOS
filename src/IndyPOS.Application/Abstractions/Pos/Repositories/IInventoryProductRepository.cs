using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IInventoryProductRepository
{
	int Add(InventoryProduct product);

	InventoryProduct GetById(int id);

	InventoryProduct GetByBarcode(string barcode);

	IEnumerable<InventoryProduct> GetProductsByCategoryId(int id);

	IEnumerable<InventoryProduct> GetProductsByDescriptionKeyword(string keyword);

	IEnumerable<InventoryProduct> GetProductsByBrandKeyword(string keyword);

	bool Update(InventoryProduct product);

	bool UpdateProductQuantityById(int id, int quantity);

	bool RemoveById(int id);

	bool Remove(InventoryProduct product);

	int GetProductBarcodeCounter();

	bool UpdateProductBarcodeCounter(int counter);
}