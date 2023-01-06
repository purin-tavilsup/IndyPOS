namespace IndyPOS.Application.Common.Interfaces;

public interface IPayLaterPaymentHelper
{
    IList<IPayLaterPayment> GetPayLaterPayments();

    IPayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId);

    IPayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId);

    IEnumerable<IPayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime startDate, DateTime endDate);

    void UpdatePayLaterPayment(IPayLaterPayment payLaterPayment);
}