using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Extensions;

public static class PaymentsExtensions
{
	public static bool HasPayLaterPayment(this IEnumerable<Payment> payments)
	{
		return payments.Any(x =>
			string.Equals(x.Method, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase)
			|| (x.Method is null && x.PaymentTypeId == (int)PaymentType.PayLater));
	}
}