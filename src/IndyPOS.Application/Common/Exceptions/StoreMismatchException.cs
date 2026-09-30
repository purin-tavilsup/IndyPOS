namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A store sent cloud sync data for a store other than the one its token authenticates. Rejected
/// whole, before anything is stored, so a store can only ever write its own data.
/// </summary>
public class StoreMismatchException(string message) : Exception(message);
