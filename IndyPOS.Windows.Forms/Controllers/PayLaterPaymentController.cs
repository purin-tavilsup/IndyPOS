using IndyPOS.Application.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.Controllers;

public class PayLaterPaymentController : IPayLaterPaymentController
{
	private readonly IPayLaterPaymentHelper _payLaterPaymentHelper;

	public PayLaterPaymentController(IPayLaterPaymentHelper payLaterPaymentHelper)
	{
		_payLaterPaymentHelper = payLaterPaymentHelper;
	}

	public IList<IPayLaterPayment> GetPayLaterPayments()
	{
		return _payLaterPaymentHelper.GetPayLaterPayments();
	}

	public IPayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId)
	{
		return _payLaterPaymentHelper.GetPayLaterPaymentByInvoiceId(invoiceId);
	}

	public IPayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId)
	{
		return _payLaterPaymentHelper.GetPayLaterPaymentByPaymentId(paymentId);
	}

	public void UpdatePayLaterPayment(IPayLaterPayment payLaterPayment)
	{
		_payLaterPaymentHelper.UpdatePayLaterPayment(payLaterPayment);
	}
}