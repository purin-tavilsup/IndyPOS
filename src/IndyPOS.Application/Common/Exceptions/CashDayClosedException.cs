namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// The entry belongs to a day other than today. Past days are closed records: a mismatch is raised
/// and resolved the same day. Mapped to 409.
/// </summary>
public class CashDayClosedException(DateOnly businessDate)
    : Exception($"แก้ไขได้เฉพาะรายการของวันนี้เท่านั้น (รายการนี้เป็นของวันที่ {businessDate:yyyy-MM-dd})");
