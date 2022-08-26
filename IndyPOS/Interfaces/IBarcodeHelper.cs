using System.Drawing;

namespace IndyPOS.Interfaces
{
    public interface IBarcodeHelper
	{
		string GenerateEan13Barcode(int productCategoryId, int productNumber);

		Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin);
	}
}
