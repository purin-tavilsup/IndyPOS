using CsvHelper.Configuration.Attributes;

namespace IndyPOS.Application.Common.Models;

public class CashFlowData
{
	[Name("วันที่")]
	public string Date { get; set; } = string.Empty;

	[Name("เวลา")]
	public string Time { get; set; } = string.Empty;

	[Name("ยอดขาย สินค้าเบ็ดเตล็ด")]
	public decimal GeneralProductsTotal { get; init; }

	[Name("ยอดขาย สินค้าฮาร์ดแวร์")]
	public decimal HardwareProductsTotal { get; init; }

	[Name("ยอดรวม เงินสด")]
	public decimal SalesTotalWithoutPayLaterPayments { get; init; }

	[Name("ยอดรวม เงินโอน")]
	public decimal MoneyTransferTotal { get; init; }

	[Name("ยอดรวม บัตรสวัสดิการ")]
	public decimal WelfareCardTotal { get; init; }

	[Name("ยอดรวม ลงบัญชี สินค้าเบ็ดเตล็ด")]
	public decimal PayLaterTotalForGeneralProducts {get; init; }

	[Name("ยอดรวม ลงบัญชี สินค้าฮาร์ดแวร์")]
	public decimal PayLaterTotalForHardwareProducts {get; init; }

	[Ignore]
	public IEnumerable<Change> Changes { get; init; } = [];

	[Ignore]
	public IEnumerable<PayLaterPayment> PaidPayLaterPayments { get; init; } = [];

	[Ignore]
	public IList<PayLaterPayment> PayLaterPayments { get; init; } = [];

	[Ignore]
	public IEnumerable<Payout> Payouts { get; init; } = [];

	[Name("ยอดรวม เงินทอน")]
	public decimal ChangesTotal => Changes.Sum(x => x.Amount);

	[Name("ยอดรวม รายจ่าย")]
	public decimal PayoutsTotal => Payouts.Sum(x => x.Amount);

	[Name("ยอดรวม ลูกค้าชำระหนี้")]
	public decimal ReceivedPayLaterPaymentsTotal => PaidPayLaterPayments.Sum(x => x.Amount);

	[Name("จำนวนนับ ธนบัตร 1000")]
	public int BankNote1000Count { get; init; }

	[Name("จำนวนนับ ธนบัตร 500")]
	public int BankNote500Count { get; init; }

	[Name("จำนวนนับ ธนบัตร 100")]
	public int BankNote100Count { get; init; }

	[Name("จำนวนนับ ธนบัตร 50")]
	public int BankNote50Count { get; init; }

	[Name("จำนวนนับ ธนบัตร 20")]
	public int BankNote20Count { get; init; }

	[Name("จำนวนนับ เหรียญ 10")]
	public int Coin10Count { get; init; }

	[Name("จำนวนนับ เหรียญ 5")]
	public int Coin5Count { get; init; }

	[Name("จำนวนนับ เหรียญ 2")]
	public int Coin2Count { get; init; }

	[Name("จำนวนนับ เหรียญ 1")]
	public int Coin1Count { get; init; }

	[Name("ยอดรวม ธนบัตร 1000")]
	public decimal BankNote1000Total => BankNote1000Count * 1000;

	[Name("ยอดรวม ธนบัตร 500")]
	public decimal BankNote500Total  => BankNote500Count * 500;

	[Name("ยอดรวม ธนบัตร 100")]
	public decimal BankNote100Total => BankNote100Count * 100;

	[Name("ยอดรวม ธนบัตร 50")]
	public decimal BankNote50Total => BankNote50Count * 50;

	[Name("ยอดรวม ธนบัตร 20")]
	public decimal BankNote20Total => BankNote20Count * 20;

	[Name("ยอดรวม เหรียญ 10")]
	public decimal Coin10Total => Coin10Count * 10;

	[Name("ยอดรวม เหรียญ 5")]
	public decimal Coin5Total => Coin5Count * 5;

	[Name("ยอดรวม เหรียญ 2")]
	public decimal Coin2Total => Coin2Count * 2;

	[Name("ยอดรวม เหรียญ 1")]
	public decimal Coin1Total => Coin1Count;
}