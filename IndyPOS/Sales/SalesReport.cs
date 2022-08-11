namespace IndyPOS.Sales
{
	public class SalesReport
    {
		public decimal InvoicesTotal { get; set; }

		public decimal InvoicesTotalWithoutAr { get; set; }
			
		public decimal GeneralProductsTotal { get; set; }
			
		public decimal HardwareProductsTotal { get; set; }

		public decimal ArTotalForGeneralProducts { get; set; }

		public decimal ArTotalForHardwareProducts { get; set; }

		public decimal GeneralProductsTotalWithoutAr { get; set; }

		public decimal HardwareProductsTotalWithoutAr { get; set; }
    }
}
