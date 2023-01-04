namespace IndyPOS.Application.Interfaces;

public interface IPayment
{
	int PaymentTypeId { get; }

	int Priority { get; }

	decimal Amount { get; }

	string Note { get; }
}