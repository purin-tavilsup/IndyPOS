using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Presentation.SalesHistory;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>The three cash-entry lists over <see cref="IStoreHubClient"/>.</summary>
public static class CashEntryApis
{
    public static ICashEntryApi Payouts(IStoreHubClient client, StoreClock clock) => new PayoutApi(client, clock);

    public static ICashEntryApi Floats(IStoreHubClient client, StoreClock clock) => new FloatApi(client, clock);

    public static ICashEntryApi DebtRepayments(IStoreHubClient client, StoreClock clock) => new DebtRepaymentApi(client, clock);

    private sealed class PayoutApi(IStoreHubClient client, StoreClock clock) : ICashEntryApi
    {
        public bool HasCategory => true;

        public async Task<IReadOnlyList<CashEntryRow>> ListAsync(DateOnly day, CancellationToken cancellationToken) =>
            (await client.GetCashPayoutsAsync(day, cancellationToken))
            .Select(p => new CashEntryRow(p.Id, clock.ToStoreTime(p.CreatedUtc), p.Description, p.Category, p.Amount))
            .ToList();

        public Task AddAsync(CashEntryInput input, CancellationToken cancellationToken) =>
            client.AddCashPayoutAsync(new AddCashPayoutRequest(input.Amount, input.Category, input.Text), cancellationToken);

        public Task EditAsync(Guid id, CashEntryInput input, CancellationToken cancellationToken) =>
            client.EditCashPayoutAsync(id, new EditCashPayoutRequest(input.Amount, input.Category, input.Text), cancellationToken);

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => client.DeleteCashPayoutAsync(id, cancellationToken);
    }

    private sealed class FloatApi(IStoreHubClient client, StoreClock clock) : ICashEntryApi
    {
        public bool HasCategory => false;

        public async Task<IReadOnlyList<CashEntryRow>> ListAsync(DateOnly day, CancellationToken cancellationToken) =>
            (await client.GetCashFloatsAsync(day, cancellationToken))
            .Select(f => new CashEntryRow(f.Id, clock.ToStoreTime(f.CreatedUtc), f.Description, null, f.Amount))
            .ToList();

        public Task AddAsync(CashEntryInput input, CancellationToken cancellationToken) =>
            client.AddCashFloatAsync(new AddCashFloatRequest(input.Amount, input.Text), cancellationToken);

        public Task EditAsync(Guid id, CashEntryInput input, CancellationToken cancellationToken) =>
            client.EditCashFloatAsync(id, new EditCashFloatRequest(input.Amount, input.Text), cancellationToken);

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => client.DeleteCashFloatAsync(id, cancellationToken);
    }

    private sealed class DebtRepaymentApi(IStoreHubClient client, StoreClock clock) : ICashEntryApi
    {
        public bool HasCategory => false;

        public async Task<IReadOnlyList<CashEntryRow>> ListAsync(DateOnly day, CancellationToken cancellationToken) =>
            (await client.GetDebtRepaymentsAsync(day, cancellationToken))
            .Select(r => new CashEntryRow(r.Id, clock.ToStoreTime(r.CreatedUtc), r.CustomerName, null, r.Amount))
            .ToList();

        public Task AddAsync(CashEntryInput input, CancellationToken cancellationToken) =>
            client.AddDebtRepaymentAsync(new AddDebtRepaymentRequest(input.Text, input.Amount), cancellationToken);

        public Task EditAsync(Guid id, CashEntryInput input, CancellationToken cancellationToken) =>
            client.EditDebtRepaymentAsync(id, new EditDebtRepaymentRequest(input.Text, input.Amount), cancellationToken);

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => client.DeleteDebtRepaymentAsync(id, cancellationToken);
    }
}
