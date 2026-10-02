namespace IndyPOS.Application.Common.Exceptions;

/// <summary>A malformed date, range, page or bill number on a /sales request. Mapped to 400.</summary>
public class SalesQueryValidationException(string message) : Exception(message);
