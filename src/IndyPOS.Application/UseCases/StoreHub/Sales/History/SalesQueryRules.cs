using System.Globalization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Validation;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Input rules for /sales. Each failure is a Thai 400, never a 500 from deeper down.</summary>
public static class SalesQueryRules
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
    public const string DateFormat = DateRangeRule.DateFormat;

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
        if (DateRangeRule.FindViolation(from, to) is { } reason)
            throw new SalesQueryValidationException(reason);
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
