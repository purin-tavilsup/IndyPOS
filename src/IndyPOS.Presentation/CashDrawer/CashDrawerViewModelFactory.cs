using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.SalesHistory;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>
/// The cash drawer ViewModel for whoever is logged in when the screen opens. The screen passes the
/// store's features, which it already reads once per run; null means they could not be read.
/// </summary>
public sealed class CashDrawerViewModelFactory(IStoreHubClient client, StoreClock clock, ICurrentUser currentUser)
{
    public const string NotLoggedIn = "No user is logged in on this till.";

    public CashDrawerViewModel Create(StoreFeaturesDto? features) =>
        new(client, clock, CashDrawerAccess.For(currentUser.User ?? throw new InvalidOperationException(NotLoggedIn)), features);
}
