using System.Drawing;

namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeGeneratorService
{
	// GenerateEan13Barcode(int productCategoryId, int productNumber) was removed with the
	// categories epic: it embedded a two-digit category ID in the barcode, and categories are
	// now store-scoped string codes. Barcodes come from StoreHub's generator instead.

	Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin);
}