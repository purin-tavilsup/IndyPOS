using FluentAssertions;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class Ean13BarcodeTests
{
    [Theory]
    // These two are REAL barcodes printed on stock in the GeneralHardware store, under the
    // legacy "200 + category + number" scheme. Their printed check digits are the ground
    // truth for this algorithm: if it cannot reproduce them, the scanner will reject our
    // labels. 2001000000012 = 200|10|0000001|2 and 2005000000027 = 200|50|0000002|7.
    [InlineData("200100000001", 2)]
    [InlineData("200500000002", 7)]
    public void CalculateCheckDigit_WithRealStoreLabels_ShouldMatchThePrintedCheckDigit(
        string twelveDigits, int expected)
    {
        Ean13Barcode.CalculateCheckDigit(twelveDigits).Should().Be(expected);
    }

    [Fact]
    public void CalculateCheckDigit_WhenTheRemainderIsZero_ShouldReturnZeroNotTen()
    {
        // The classic off-by-one in this algorithm: 10 - 0 = 10 is not a digit. Weighted
        // sum here is 2 + (6 * 3) = 20, so the check digit must be 0.
        Ean13Barcode.CalculateCheckDigit("200000000006").Should().Be(0);
    }

    [Theory]
    [InlineData("20010000000")]    // 11 digits
    [InlineData("2001000000012")]  // 13 digits - the caller must strip the check digit first
    [InlineData("")]
    public void CalculateCheckDigit_WithoutExactlyTwelveDigits_ShouldThrow(string notTwelveDigits)
    {
        var act = () => Ean13Barcode.CalculateCheckDigit(notTwelveDigits);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCheckDigit_WithANonDigit_ShouldThrow()
    {
        var act = () => Ean13Barcode.CalculateCheckDigit("20010000000X");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForStoreProduct_ShouldProduceAThirteenDigitBarcode()
    {
        // 200 | 01 | 0000003 | check. ZXing's EAN-13 writer accepts only 12 or 13 digits,
        // so anything else crashes the label preview - the bug this method exists to fix.
        var barcode = Ean13Barcode.ForStoreProduct(storeCode: 1, sequence: 3);

        barcode.Should().Be("2000100000038");
        barcode.Should().HaveLength(13);
    }

    [Fact]
    public void ForStoreProduct_ShouldUseTheInStoreRangePrefix()
    {
        // GS1 reserves 200-299 for restricted circulation, i.e. barcodes a shop assigns
        // itself. The store's existing stock already uses 200, so ours must too.
        Ean13Barcode.ForStoreProduct(storeCode: 7, sequence: 42)
            .Should().StartWith(Ean13Barcode.InStorePrefix);
    }

    [Theory]
    [InlineData(1, 3, "2000100000038")]
    [InlineData(2, 1, "2000200000013")]
    [InlineData(1, 9_999_999, "2000199999992")]
    public void ForStoreProduct_ShouldZeroPadBothFields(int storeCode, int sequence, string expected)
    {
        Ean13Barcode.ForStoreProduct(storeCode, sequence).Should().Be(expected);
    }

    [Fact]
    public void ForStoreProduct_ShouldProduceAValidatingBarcode()
    {
        // Round-trip: recomputing the check digit over our own first 12 digits must agree.
        var barcode = Ean13Barcode.ForStoreProduct(storeCode: 3, sequence: 12345);

        var recomputed = Ean13Barcode.CalculateCheckDigit(barcode[..12]);

        recomputed.Should().Be(int.Parse(barcode[12..]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void ForStoreProduct_WithAStoreCodeOutsideTwoDigits_ShouldThrow(int storeCode)
    {
        // A 3-digit store code would silently push the barcode to 14 digits, which the
        // writer rejects far away from here. Fail at the source instead.
        var act = () => Ean13Barcode.ForStoreProduct(storeCode, sequence: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000_000)]
    public void ForStoreProduct_WithASequenceOutsideSevenDigits_ShouldThrow(int sequence)
    {
        var act = () => Ean13Barcode.ForStoreProduct(storeCode: 1, sequence);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ForStoreProduct_ForTheThreeLiveStores_ShouldNotCollideWithLegacyCategoryBarcodes(
        int storeCode)
    {
        // Legacy barcodes put the CATEGORY id in these two digits, and category ids run
        // 10-54. The three live stores use codes 1-3, which zero-pad to 01-03 and so can
        // never land in that band. A store code of 10-54 WOULD collide; the unique index
        // on (store_id, barcode) is the backstop, but keep live codes single-digit.
        var barcode = Ean13Barcode.ForStoreProduct(storeCode, sequence: 1);

        var categoryField = int.Parse(barcode.Substring(3, 2));

        categoryField.Should().BeLessThan(10);
    }
}
