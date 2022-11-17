using System.Drawing;

namespace IndyPOS.Facade.Interfaces;

public interface IBarcodeUtility
{
	string GenerateEan13Barcode(int productCategoryId, int productNumber);

	Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin);
}