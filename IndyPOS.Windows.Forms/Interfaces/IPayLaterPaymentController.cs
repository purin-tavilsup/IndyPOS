using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Windows.Forms.Interfaces;

public interface IPayLaterPaymentController
{
	IList<IPayLaterPayment> GetPayLaterPayments();

	IPayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId);

	IPayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId);

	void UpdatePayLaterPayment(IPayLaterPayment payLaterPayment);
}