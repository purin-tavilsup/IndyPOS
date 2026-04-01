namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// PayLater item DTO for list and detail views.
/// </summary>
public record PayLaterDto(
    Guid Id,
    Guid PaymentId,
    Guid InvoiceId,
    string Description,
    decimal PayLaterAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    bool IsCompleted,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc)
{
    /// <summary>
    /// Determines if recording the given payment amount would complete this PayLater.
    /// </summary>
    public bool WouldBeCompletedWith(decimal paymentAmount)
        => PaidAmount + paymentAmount >= PayLaterAmount;
}

/// <summary>
/// Response for GetPayLater query with summary stats.
/// </summary>
public record GetPayLaterResponse(
    decimal TotalOutstanding,
    decimal TotalPaid,
    int ActiveCount,
    int CompletedCount,
    IReadOnlyList<PayLaterDto> Items);

/// <summary>
/// Request body for recording a payment against a pay-later account.
/// </summary>
public record RecordPaymentRequest(decimal PaymentAmount);
