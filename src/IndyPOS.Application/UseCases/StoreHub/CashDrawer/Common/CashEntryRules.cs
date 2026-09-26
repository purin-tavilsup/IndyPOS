using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>Boundary rules for hand-typed cash input. Messages are Thai: the cashier reads them.</summary>
public static class CashEntryRules
{
    /// <summary>Well inside numeric(18,2), far above any real drawer movement.</summary>
    public const decimal MaxAmount = 9_999_999.99m;
    public const int MaxDescriptionLength = 500;
    public const int MaxCustomerNameLength = 200;

    public static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
            throw new CashEntryValidationException("จำนวนเงินต้องมากกว่า 0");

        if (amount > MaxAmount)
            throw new CashEntryValidationException($"จำนวนเงินต้องไม่เกิน {MaxAmount:N2}");

        // The column keeps 2 decimals; rejecting beats silently rounding the cashier's number.
        if (decimal.Round(amount, 2) != amount)
            throw new CashEntryValidationException("จำนวนเงินมีทศนิยมได้ไม่เกิน 2 ตำแหน่ง");
    }

    public static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > MaxDescriptionLength)
            throw new CashEntryValidationException($"รายละเอียดยาวเกิน {MaxDescriptionLength} ตัวอักษร");

        return trimmed;
    }

    public static string NormalizeCustomerName(string? customerName)
    {
        var trimmed = customerName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new CashEntryValidationException("กรุณาระบุชื่อลูกค้า");

        if (trimmed.Length > MaxCustomerNameLength)
            throw new CashEntryValidationException($"ชื่อลูกค้ายาวเกิน {MaxCustomerNameLength} ตัวอักษร");

        return trimmed;
    }

    public static PayoutCategory EnsureDefined(PayoutCategory category)
    {
        // The JSON converter accepts integers, so an out-of-range number can arrive here.
        if (!Enum.IsDefined(category))
            throw new CashEntryValidationException("หมวดหมู่รายจ่ายไม่ถูกต้อง");

        return category;
    }

    public static void EnsureValidCounts(params int[] counts)
    {
        if (counts.Any(c => c < 0))
            throw new CashEntryValidationException("จำนวนนับต้องไม่ติดลบ");
    }
}
