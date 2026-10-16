using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public sealed class ReceiptImagePrinterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "indypos-receipt-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void PrintReceipt_WithACopy_MarksTheFileAsACopy()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt() with { Reprint = new ReceiptReprint(new DateTime(2026, 10, 16, 9, 5, 0), "Somchai") });

        Path.GetFileName(printer.LastSavedPath).Should()
                                               .StartWith("0000001042-copy");
    }

    [Fact]
    public void PrintReceipt_WithASale_SavesAPngNamedByTheBillNumber()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt());

        Path.GetFileName(printer.LastSavedPath).Should()
                                               .MatchRegex(@"^0000001042-\d{14}\.png$");
    }

    [Fact]
    public void PrintReceipt_WithASale_WritesTheFile()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt());

        File.Exists(printer.LastSavedPath).Should()
                                          .BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ReceiptImagePrinter Printer()
    {
        var config = new Mock<IStoreConfigurationService>();
        config.Setup(c => c.Get()).Returns(new StoreConfiguration { StoreName = "Test Store" });

        return new ReceiptImagePrinter(config.Object, NullLogger<ReceiptImagePrinter>.Instance, _directory);
    }

    private static ReceiptDocument Receipt() => new(
        1042, new DateTime(2026, 10, 15, 14, 30, 0), "Malee Cashier",
        [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        [new ReceiptPayment("เงินสด", 100m, null)],
        100m, 100m, 0m, false);
}
