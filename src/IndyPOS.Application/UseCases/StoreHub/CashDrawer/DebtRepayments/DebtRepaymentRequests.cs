namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddDebtRepaymentRequest(string? CustomerName, decimal Amount);

public record EditDebtRepaymentRequest(string? CustomerName, decimal Amount);
