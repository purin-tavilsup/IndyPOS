using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddCashPayoutRequest(decimal Amount, PayoutCategory? Category, string? Description);

public record EditCashPayoutRequest(decimal Amount, PayoutCategory? Category, string? Description);
