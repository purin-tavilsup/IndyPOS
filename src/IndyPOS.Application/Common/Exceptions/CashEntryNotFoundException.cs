namespace IndyPOS.Application.Common.Exceptions;

/// <summary>The entry is unknown or soft-deleted. Mapped to 404.</summary>
public class CashEntryNotFoundException(Guid id)
    : Exception($"ไม่พบรายการ ({id})");
