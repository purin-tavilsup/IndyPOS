namespace IndyPOS.DataAccess.Models
{
	public class PackPrice
	{
		public int Id { get; set; }

		public string Barcode { get; set; }

		public int QuantityPerPack { get; set; }

		public decimal UnitPricePerPack { get; set; }

		public string DateCreated { get; set; }
	}
}
