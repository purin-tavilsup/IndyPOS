using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentMethod method, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default);
}
