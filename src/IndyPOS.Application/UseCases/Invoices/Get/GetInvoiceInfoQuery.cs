using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public record GetInvoiceInfoQuery(int InvoiceId) : IQuery<IInvoiceInfo>;