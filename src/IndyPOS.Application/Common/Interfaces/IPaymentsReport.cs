namespace IndyPOS.Application.Common.Interfaces;

public interface IPaymentsReport
{
	public decimal MoneyTransferTotal { get; set; }

	public decimal FiftyFiftyTotal { get; set; }

	public decimal M33WeLoveTotal { get; set; }

	public decimal WeWinTotal { get; set; }

	public decimal WelfareCardTotal { get; set; }

	public decimal PayLaterTotal { get; set; }
}