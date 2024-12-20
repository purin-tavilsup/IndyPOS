﻿using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public record GetInvoiceInfoQuery(int InvoiceId) : IQuery<IInvoiceInfo>;