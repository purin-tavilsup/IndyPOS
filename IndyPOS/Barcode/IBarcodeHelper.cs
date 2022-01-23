using System.Drawing;

namespace IndyPOS.Barcode
{
    public interface IBarcodeHelper
	{
		string GenerateEan13Barcode(int productCategoryId, int productNumber);

		Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin);
	}
}
