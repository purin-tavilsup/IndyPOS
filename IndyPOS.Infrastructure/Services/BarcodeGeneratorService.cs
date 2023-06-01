using IndyPOS.Application.Common.Interfaces;
using System.Runtime.Versioning;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace IndyPOS.Infrastructure.Services;

[type: SupportedOSPlatform("windows")]
public class BarcodeGeneratorService : IBarcodeGeneratorService
{
    public string GenerateEan13Barcode(int productCategoryId, int productNumber)
    {
        const int usageAreaCode = 200;
        var twelveDigitCode = $"{usageAreaCode:000}{productCategoryId:00}{productNumber:0000000}";
        var checkDigit = CalculateCheckDigit(twelveDigitCode);

        return $"{twelveDigitCode}{checkDigit}";
    }

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

    private static int CalculateCheckDigit(string twelveDigitCode)
    {
        if (twelveDigitCode.Length != 12)
            throw new ArgumentException("A 12-digit code is required for calculating checksum for EAN-13 barcode");

        var sumValuesInOddPosition = 0;
        var sumValuesInEvenPosition = 0;

        for (var index = 1; index <= 12; index++)
        {
            var digitCharacter = twelveDigitCode[index - 1];

            if (!int.TryParse($"{digitCharacter}", out var digitValue))
                throw new ArgumentException($"Failed to calculate checksum because '{twelveDigitCode}' contains invalid digit '{digitCharacter}'");

            if (IsEven(index))
            {
                sumValuesInEvenPosition += digitValue;
            }
            else
            {
                sumValuesInOddPosition += digitValue;
            }
        }

        var checkDigit = (sumValuesInOddPosition + sumValuesInEvenPosition * 3) % 10;

        return checkDigit == 0 ? checkDigit : 10 - checkDigit;
    }

    private static bool IsEven(int value)
    {
        return value % 2 == 0;
    }
}