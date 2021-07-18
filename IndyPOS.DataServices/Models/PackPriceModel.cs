﻿namespace IndyPOS.DataAccess.Models
{
	public class PackPriceModel
	{
		public int Id { get; set; }

		public string Barcode { get; set; }

		public int QuantityPerPack { get; set; }

		public string UnitPricePerPack { get; set; }

		public string DateCreated { get; set; }
	}
}
