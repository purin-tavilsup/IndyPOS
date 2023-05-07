using System.Drawing;

namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeGeneratorService
{
	string GenerateEan13Barcode(int productCategoryId, int productNumber);

	Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin);
}