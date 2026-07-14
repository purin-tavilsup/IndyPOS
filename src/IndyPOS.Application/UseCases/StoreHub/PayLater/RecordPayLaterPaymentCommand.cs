using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// Command to record a payment against a PayLater account.
/// </summary>
public record RecordPayLaterPaymentCommand(
    Guid PayLaterId,
    decimal PaymentAmount) : ICommand<PayLaterDto>;

/// <summary>
/// Handler for RecordPayLaterPaymentCommand.
/// </summary>
public class RecordPayLaterPaymentCommandHandler : ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto>
{
    private readonly IPayLaterRepository _payLaterRepository;
    private readonly IStoreIdentityService _storeIdentity;

    public RecordPayLaterPaymentCommandHandler(
        IPayLaterRepository payLaterRepository,
        IStoreIdentityService storeIdentity)
    {
        _payLaterRepository = payLaterRepository;
        _storeIdentity = storeIdentity;
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

        // Record the payment
        var newPaidAmount = payLater.PaidAmount + command.PaymentAmount;
        payLater.PaidAmount = newPaidAmount;
        payLater.IsCompleted = newPaidAmount >= payLater.PayLaterAmount;
        payLater.LastModifiedUtc = DateTime.UtcNow;

        await _payLaterRepository.UpdateAsync(payLater, cancellationToken);

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
}
