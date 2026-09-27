namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Cash a customer paid towards a debt (ลูกค้าชำระหนี้). A hand-typed list: deliberately NOT linked
/// to the customer's PayLater row.
/// </summary>
public class DebtRepayment : CashDrawerEntry
{
    public string CustomerName { get; set; } = default!;
}
