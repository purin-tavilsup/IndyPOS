using System.Globalization;

namespace IndyPOS.Application.Common.Validation;

/// <summary>
/// The one date-range rule for every query that takes a from/to pair: /sales and the dated /reports
/// routes, so the two cannot drift. A refusal is a Thai sentence for the cashier, never a 500 from
/// deeper down.
/// </summary>
public static class DateRangeRule
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// The dates a query may ask for. Well before any store opened and well after any till will run,
    /// yet far enough from DateOnly's own limits that ReportDateRange.ToUtcRange -- which adds a day
    /// and shifts by the store's offset -- can never overflow into a 500.
    /// </summary>
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);
    public static readonly DateOnly LatestDate = new(2099, 12, 31);

    public const string ToBeforeFromMessage = "วันที่เริ่มต้นต้องไม่อยู่หลังวันที่สิ้นสุด";

    public static readonly string OutOfBoundsMessage =
        $"วันที่ต้องอยู่ระหว่าง {EarliestDate.ToString(DateFormat, CultureInfo.InvariantCulture)} " +
        $"ถึง {LatestDate.ToString(DateFormat, CultureInfo.InvariantCulture)}";

    /// <summary>Why the range is refused, in Thai, or null when it is valid.</summary>
    public static string? FindViolation(DateOnly from, DateOnly to)
    {
        if (from < EarliestDate || to > LatestDate)
            return OutOfBoundsMessage;

        return to < from ? ToBeforeFromMessage : null;
    }
}
