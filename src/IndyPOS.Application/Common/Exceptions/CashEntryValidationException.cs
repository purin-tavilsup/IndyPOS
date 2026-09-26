namespace IndyPOS.Application.Common.Exceptions;

/// <summary>Input broke a cash-drawer rule. The message is shown to the cashier. Mapped to 400.</summary>
public class CashEntryValidationException(string message) : Exception(message);
