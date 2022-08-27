using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Configurations.Models
{
	[ExcludeFromCodeCoverage]
    public class UserConfiguration
    {
		public string StoreFullName { get; set; }
		public string StoreName { get; set; }
		public string StoreAddressLine1 { get; set; }
		public string StoreAddressLine2 { get; set; }
		public string StorePhoneNumber { get; set; }
		public string PrinterName { get; set; }
		public string BarcodeScannerPortName { get; set; }
		public string BackupDbDirectory { get; set; }
		public string BarcodeDirectory { get; set; }
		public string DataFeedKey { get; set; }
		public bool DataFeedEnabled { get; set; }
    }
}
