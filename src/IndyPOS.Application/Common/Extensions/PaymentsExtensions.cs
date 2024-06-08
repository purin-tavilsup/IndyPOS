using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Enums;

namespace IndyPOS.Application.Common.Extensions;

public static class PaymentsExtensions
{
	public static bool HasPayLayerPayment(this IEnumerable<Payment> payments)
	{
		return payments.Any(x => x.PaymentTypeId == (int)PaymentType.PayLater);
	}
}