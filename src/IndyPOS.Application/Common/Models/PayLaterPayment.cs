namespace IndyPOS.Application.Common.Models;

public class PayLaterPayment
{
	public string Date { get; set; }
	public string AccountName { get; set; }
	public decimal Amount { get; set; }
}