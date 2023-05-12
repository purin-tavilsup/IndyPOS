namespace IndyPOS.Application.Common.Interfaces;

public interface IPayLaterPaymentHelper
{
    IList<IPayLaterPayment> GetPayLaterPayments();

    IPayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId);

    IPayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId);

    IEnumerable<IPayLaterPayment> GetPayLaterPaymentsByDateRange(DateOnly startDate, DateOnly endDate);

    void UpdatePayLaterPayment(IPayLaterPayment payLaterPayment);
}