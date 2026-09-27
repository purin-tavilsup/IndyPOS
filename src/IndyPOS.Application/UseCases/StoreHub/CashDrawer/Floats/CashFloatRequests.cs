namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddCashFloatRequest(decimal Amount, string? Description);

public record EditCashFloatRequest(decimal Amount, string? Description);
