using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// The DEBUG till's printer: each receipt is saved as a PNG, drawn exactly as the thermal printer
/// would, instead of being sent to a printer the machine does not have. TillHost registers it in
/// DEBUG builds only, which is the till's whole print gate.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ReceiptImagePrinter : IReceiptPrinterService
{
    private const int Width = 220;
    private const int TallestReceipt = 4000;
    private const int BottomMargin = 10;

    private readonly ReceiptHeader _header;
    private readonly ILogger<ReceiptImagePrinter> _logger;
    private readonly string _directory;

    public ReceiptImagePrinter(IStoreConfigurationService storeConfiguration, ILogger<ReceiptImagePrinter> logger)
        : this(storeConfiguration, logger, DefaultDirectory)
    {
    }

    public ReceiptImagePrinter(IStoreConfigurationService storeConfiguration, ILogger<ReceiptImagePrinter> logger,
                               string directory)
    {
        _header = ReceiptHeaders.From(storeConfiguration.Get());
        _logger = logger;
        _directory = directory;
    }

    public static string DefaultDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndyPOS", "receipts");

    public string? LastSavedPath { get; private set; }

    public void PrintReceipt(ReceiptDocument document)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, FileName(document));

        using var page = new Bitmap(Width, TallestReceipt);
        int bottom;
        using (var graphics = Graphics.FromImage(page))
        {
            graphics.Clear(Color.White);
            bottom = ReceiptRenderer.Draw(graphics, ReceiptLayout.Rows(_header, document, CultureInfo.CurrentCulture));
        }

        using var receipt = page.Clone(new Rectangle(0, 0, Width, Math.Min(bottom + BottomMargin, TallestReceipt)), page.PixelFormat);
        receipt.Save(path, ImageFormat.Png);

        LastSavedPath = path;
        _logger.LogInformation("DEBUG build: receipt {InvoiceNumber} saved as {Path}, not printed", document.InvoiceNumber, path);
    }

    // The time keeps two copies of one bill apart.
    private static string FileName(ReceiptDocument document) =>
        $"{document.InvoiceNumber.ToString("0000000000", CultureInfo.InvariantCulture)}{(document.IsCopy ? "-copy" : "")}" +
        $"-{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.png";
}
