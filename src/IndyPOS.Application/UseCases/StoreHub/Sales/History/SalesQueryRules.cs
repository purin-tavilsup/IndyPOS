using System.Globalization;
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Input rules for /sales. Each failure is a Thai 400, never a 500 from deeper down.</summary>
public static class SalesQueryRules
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// The dates a list may ask for. Well before any store opened and well after any till will run,
    /// yet far enough from DateOnly's own limits that ReportDateRange.ToUtcRange -- which adds a day
    /// and shifts by the store's offset -- can never overflow into a 500.
    /// </summary>
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);
    public static readonly DateOnly LatestDate = new(2099, 12, 31);

    /// <summary>
    /// InvariantCulture is load-bearing: the server may run on a th-TH machine, whose Buddhist
    /// calendar would read 2026 as a Buddhist-era year.
    /// </summary>
    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new SalesQueryValidationException($"วันที่ไม่ถูกต้อง: {value} (ใช้รูปแบบ {DateFormat})");
    }

    public static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (from < EarliestDate || to > LatestDate)
            throw new SalesQueryValidationException(
                $"วันที่ต้องอยู่ระหว่าง {EarliestDate.ToString(DateFormat, CultureInfo.InvariantCulture)} " +
                $"ถึง {LatestDate.ToString(DateFormat, CultureInfo.InvariantCulture)}");

        if (to < from)
            throw new SalesQueryValidationException("วันที่เริ่มต้นต้องไม่อยู่หลังวันที่สิ้นสุด");
    }

    public static void EnsureValidPage(int page, int pageSize)
    {
        if (page < FirstPage)
            throw new SalesQueryValidationException("หน้าต้องเริ่มที่ 1");

        if (pageSize is < 1 or > MaxPageSize)
            throw new SalesQueryValidationException($"จำนวนต่อหน้าต้องอยู่ระหว่าง 1 ถึง {MaxPageSize}");

        if ((long)(page - 1) * pageSize > int.MaxValue)
            throw new SalesQueryValidationException("หน้าที่ขอเกินจำนวนบิลที่มีได้");
    }

    public static void EnsureValidNumber(long number)
    {
        if (number < 1)
            throw new SalesQueryValidationException(InvalidNumberMessage(number.ToString(CultureInfo.InvariantCulture)));
    }

    public static string InvalidNumberMessage(string value) => $"เลขที่บิลไม่ถูกต้อง: {value}";
}
