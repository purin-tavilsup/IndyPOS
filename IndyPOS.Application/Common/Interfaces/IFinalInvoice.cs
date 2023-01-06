namespace IndyPOS.Application.Common.Interfaces;

public interface IFinalInvoice
{
    int InvoiceId { get; }

    decimal Total { get; }

    int? CustomerId { get; }

    int UserId { get; }

    string DateCreated { get; }
}