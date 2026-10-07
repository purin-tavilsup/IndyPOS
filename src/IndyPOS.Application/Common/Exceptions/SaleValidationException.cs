namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A sale the store must not record: its payments break a rule. The message is Thai, for the
/// cashier. POST /sales answers it with 400.
/// </summary>
public class SaleValidationException(string message) : Exception(message);
