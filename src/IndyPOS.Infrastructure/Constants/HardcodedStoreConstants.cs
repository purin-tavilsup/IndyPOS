using IndyPOS.Application.Common.Interfaces;
using Enums = IndyPOS.Application.Common.Enums;

namespace IndyPOS.Infrastructure.Constants;

/// <summary>
/// Hardcoded store constants derived from application enums.
/// Replaces SQLite-based StoreConstants for StoreHub mode.
/// </summary>
public class HardcodedStoreConstants : IStoreConstants
{
    public HardcodedStoreConstants()
    {
        UserRoles = new Dictionary<int, string>
        {
            [(int)Enums.UserRole.Cashier] = nameof(Enums.UserRole.Cashier),
            [(int)Enums.UserRole.StoreManager] = nameof(Enums.UserRole.StoreManager),
            [(int)Enums.UserRole.SystemAdmin] = nameof(Enums.UserRole.SystemAdmin)
        };

        PaymentTypes = new Dictionary<int, string>
        {
            [(int)Enums.PaymentType.Cash] = nameof(Enums.PaymentType.Cash),
            [(int)Enums.PaymentType.PayLater] = nameof(Enums.PaymentType.PayLater),
            [(int)Enums.PaymentType.WelfareCard] = nameof(Enums.PaymentType.WelfareCard),
            [(int)Enums.PaymentType.M33WeLove] = nameof(Enums.PaymentType.M33WeLove),
            [(int)Enums.PaymentType.MoneyTransfer] = nameof(Enums.PaymentType.MoneyTransfer),
            [(int)Enums.PaymentType.FiftyFifty] = nameof(Enums.PaymentType.FiftyFifty),
            [(int)Enums.PaymentType.WeWin] = nameof(Enums.PaymentType.WeWin)
        };
    }

    public IReadOnlyDictionary<int, string> UserRoles { get; }

    public IReadOnlyDictionary<int, string> PaymentTypes { get; }
}
