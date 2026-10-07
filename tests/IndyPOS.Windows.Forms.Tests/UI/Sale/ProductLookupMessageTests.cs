using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Windows.Forms.UI.Sale;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Sale;

public class ProductLookupMessageTests
{
    private const string Barcode = "2002500000014";

    // Anything else (StoreHub down, a timeout) keeps its detail, so the cause can still be told apart.
    [Fact]
    public void For_WithAnotherError_KeepsTheErrorDetail()
    {
        var message = ProductLookupMessage.For(Barcode, new HttpRequestException("Connection refused"));

        message.Should()
               .Be($"ไม่พบรหัสสินค้า {Barcode} ในระบบ Error: Connection refused");
    }

    [Fact]
    public void For_WithAProductNotFound_ShowsTheThaiMessageOnly()
    {
        var message = ProductLookupMessage.For(Barcode, new ProductNotFoundException(Barcode));

        message.Should()
               .Be($"ไม่พบรหัสสินค้า {Barcode} ในระบบ");
    }
}
