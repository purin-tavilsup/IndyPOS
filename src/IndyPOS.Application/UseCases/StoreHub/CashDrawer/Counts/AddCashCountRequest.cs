namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>The nine denomination counts. No user id and no date: the server sets both.</summary>
public record AddCashCountRequest(
    int BankNote1000Count,
    int BankNote500Count,
    int BankNote100Count,
    int BankNote50Count,
    int BankNote20Count,
    int Coin10Count,
    int Coin5Count,
    int Coin2Count,
    int Coin1Count);
