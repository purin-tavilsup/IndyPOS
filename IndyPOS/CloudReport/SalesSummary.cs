using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.CloudReport
{
    internal class SalesSummary
    {
		// Change to an array of SalesSummaryData
		public decimal InvoicesTotal { get; set; }

		public decimal InvoicesTotalWithoutAr { get; set; }
			
		public decimal GeneralProductsTotal { get; set; }
			
		public decimal HardwareProductsTotal { get; set; }

		public decimal ArTotalForGeneralProducts { get; set; }

		public decimal ArTotalForHardwareProducts { get; set; }

		public decimal GeneralProductsTotalWithoutAr { get; set; }

		public decimal HardwareProductsTotalWithoutAr { get; set; }

		public decimal PaymentViaMoneyTransfer { get; set; }

		public decimal PaymentViaFiftyFifty { get; set; }

		public decimal PaymentViaM33WeLove { get; set; }

		public decimal PaymentViaWeWin { get; set; }

		public decimal PaymentViaWelfareCard { get; set; }

		public decimal PaymentViaAr { get; set; }
    }
}
