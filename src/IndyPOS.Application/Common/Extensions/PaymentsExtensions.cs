using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Extensions;

public static class PaymentsExtensions
{
	public static bool HasPayLaterPayment(this IEnumerable<Payment> payments)
	{
		return payments.Any(x => x.PaymentTypeId == (int)PaymentType.PayLater);
	}
}