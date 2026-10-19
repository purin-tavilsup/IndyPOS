using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// Command to record a payment against a PayLater account, taken by the cashier <paramref name="UserId"/>.
/// </summary>
public record RecordPayLaterPaymentCommand(
    Guid PayLaterId,
    decimal PaymentAmount,
    Guid UserId) : ICommand<PayLaterDto>;

/// <summary>
/// Handler for RecordPayLaterPaymentCommand.
/// </summary>
public class RecordPayLaterPaymentCommandHandler : ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto>
{
    private readonly IPayLaterRepository _payLaterRepository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ICashDrawerClock _clock;

    public RecordPayLaterPaymentCommandHandler(
        IPayLaterRepository payLaterRepository,
        IStoreIdentityService storeIdentity,
        ICashDrawerClock clock)
    {
        _payLaterRepository = payLaterRepository;
        _storeIdentity = storeIdentity;
        _clock = clock;
    }

    public async Task<PayLaterDto> HandleAsync(
        RecordPayLaterPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate PayLater is enabled for this store type
        if (!_storeIdentity.Features.PayLaterEnabled)
        {
            throw new InvalidOperationException(
                $"PayLater is not available for {_storeIdentity.StoreType} stores.");
        }

        var payLater = await _payLaterRepository.GetByIdAsync(command.PayLaterId, cancellationToken)
            ?? throw new PayLaterPaymentNotFoundException($"PayLater with ID {command.PayLaterId} not found.");

        if (payLater.IsCompleted)
        {
            throw new PayLaterPaymentNotUpdatedException($"PayLater {command.PayLaterId} is already completed.");
        }

        if (command.PaymentAmount <= 0)
        {
            throw new ArgumentException("Payment amount must be positive.", nameof(command.PaymentAmount));
        }

        var now = _clock.Now();
        var repayment = NewRepayment(command, now);
        ApplyRepayment(payLater, command.PaymentAmount, now.Utc);

        // Spec 2026-10-19 §3: the debt, its repayment and both cloud events land in one save, or none do.
        await _payLaterRepository.RecordRepaymentAsync(
            payLater,
            repayment,
            [PayLaterOutbox.Repaid(repayment, now.Utc), PayLaterOutbox.Changed(payLater, _storeIdentity.StoreId, now.Utc)],
            cancellationToken);

        return new PayLaterDto(
            payLater.Id,
            payLater.PaymentId,
            payLater.InvoiceId,
            payLater.Description,
            payLater.PayLaterAmount,
            payLater.PaidAmount,
            payLater.RemainingAmount,
            payLater.IsCompleted,
            payLater.CreatedUtc,
            payLater.LastModifiedUtc);
    }

    private PayLaterRepayment NewRepayment(RecordPayLaterPaymentCommand command, CashDrawerInstant now) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = _storeIdentity.StoreId,
        PayLaterId = command.PayLaterId,
        Amount = command.PaymentAmount,
        BusinessDate = now.BusinessDate,
        CreatedUtc = now.Utc,
        CreatedByUserId = command.UserId
    };

    private static void ApplyRepayment(Domain.Entities.Core.PayLater payLater, decimal amount, DateTime utcNow)
    {
        payLater.PaidAmount += amount;
        payLater.IsCompleted = payLater.PaidAmount >= payLater.PayLaterAmount;
        payLater.LastModifiedUtc = utcNow;
    }
}
