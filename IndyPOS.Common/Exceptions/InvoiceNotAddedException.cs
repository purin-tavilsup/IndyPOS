﻿namespace IndyPOS.Common.Exceptions;

public class InvoiceNotAddedException : Exception
{
    public InvoiceNotAddedException(string message) : base(message) { }
}