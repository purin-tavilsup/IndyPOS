using System.Drawing;
using ZXing;
using ZXing.Common;

namespace IndyPOS.Barcode
{
    internal class BarcodeHelper : IBarcodeHelper
    {
		public Bitmap GenerateBarcode(string barcode)
		{
			var writer = new BarcodeWriter
						 {
							 Format = BarcodeFormat.EAN_13,
							 Options = new EncodingOptions
									   {
										   Height = 150,
										   Width = 300,
										   PureBarcode = false,
										   Margin = 10,
									   }
						 };
			
			return writer.Write(barcode);
		}
    }
}
