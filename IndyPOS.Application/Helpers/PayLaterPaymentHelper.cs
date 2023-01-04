﻿using IndyPOS.Application.Adapters;
using IndyPOS.Application.Interfaces;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.Application.Helpers;

public class PayLaterPaymentHelper : IPayLaterPaymentHelper
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public PayLaterPaymentHelper(IPayLaterPaymentRepository payLaterPaymentRepository)
	{
		_payLaterPaymentRepository = payLaterPaymentRepository;
	}

	public IList<IPayLaterPayment> GetPayLaterPayments()
	{
		var results = _payLaterPaymentRepository.GetPayLaterPayments();

		return results.Select(x => new PayLaterPaymentAdapter(x) as IPayLaterPayment).ToList();
	}

	public IPayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId)
	{
		var result = _payLaterPaymentRepository.GetPayLaterPaymentByInvoiceId(invoiceId);

		return new PayLaterPaymentAdapter(result);
	}

	public IPayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId)
	{
		var result = _payLaterPaymentRepository.GetPayLaterPaymentByPaymentId(paymentId);

		return new PayLaterPaymentAdapter(result);
	}

	public IEnumerable<IPayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime startDate, DateTime endDate)
	{
		var results = _payLaterPaymentRepository.GetPayLaterPaymentsByDateRange(startDate, endDate);

		return results.Select(x => new PayLaterPaymentAdapter(x) as IPayLaterPayment);
	}

	public void UpdatePayLaterPayment(IPayLaterPayment payLaterPayment)
	{
		var isCompleted = payLaterPayment.Amount == payLaterPayment.PaidAmount;

		_payLaterPaymentRepository.UpdatePayLaterPayment(new PayLaterPayment
		{
			PaymentId = payLaterPayment.PaymentId,
			PaidAmount = payLaterPayment.PaidAmount,
			IsCompleted = isCompleted
		});
	}
}