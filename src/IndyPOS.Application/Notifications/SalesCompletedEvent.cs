using MediatR;

namespace IndyPOS.Application.Notifications;

public record SalesCompletedEvent(int InvoiceId, bool HasPayLaterPayment) : INotification;