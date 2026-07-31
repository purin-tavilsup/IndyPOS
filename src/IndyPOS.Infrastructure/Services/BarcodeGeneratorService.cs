using IndyPOS.Application.Common.Interfaces;
using System.Runtime.Versioning;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace IndyPOS.Infrastructure.Services;

[type: SupportedOSPlatform("windows")]
public class BarcodeGeneratorService : IBarcodeGeneratorService
{
    public Bitmap CreateEan13BarcodeImage(string barcode, int height, int width, int margin)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.EAN_13,
            Options = new EncodingOptions
            {
                Height = height,
                Width = width,
                PureBarcode = false,
                Margin = margin,
                GS1Format = false
            }
        };

        return writer.Write(barcode);
    }
}