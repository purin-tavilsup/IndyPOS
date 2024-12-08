using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.PaymentsReports.Create;
using IndyPOS.Application.UseCases.SalesReports.Create;
using MediatR;

namespace IndyPOS.Application.Notifications;

public class SalesCompletedEventHandler : INotificationHandler<SalesCompletedEvent>
{
	private readonly IMediator _mediator;
	private readonly IStoreConfigurationService _storeConfigurationService;
	
	public SalesCompletedEventHandler(IMediator mediator, IStoreConfigurationService storeConfigurationService)
	{
		_mediator = mediator;
		_storeConfigurationService = storeConfigurationService;
	}

	public async Task Handle(SalesCompletedEvent notification, CancellationToken cancellationToken)
	{
		var config = await _storeConfigurationService.GetAsync();

		if (!ShouldCreateReports(config))
		{
			return;
		}

		var invoiceId = notification.InvoiceId;
		var hasPayLaterPayment = notification.HasPayLaterPayment;
		
		await _mediator.Send(new CreateSalesReportCommand(invoiceId, hasPayLaterPayment), cancellationToken);
		await _mediator.Send(new CreatePaymentsReportCommand(invoiceId), cancellationToken);
	}

	private static bool ShouldCreateReports(StoreConfiguration config)
	{
		return config.CloudDatabaseEnabled ?? false;
	}
}