using IndyPOS.Utilities;

namespace IndyPOS
{
    public class Config : PersistentXml, IConfig
	{
		public string StoreFullName { get; set; }
		public string StoreName { get; set; }
		public string StoreAddressLine1 { get; set; }
		public string StoreAddressLine2 { get; set; }
		public string StorePhoneNumber { get; set; }
		public string PrinterName { get; set; }
		public string BarcodeScannerPortName { get; set; }
		public string ReportDirectory { get; set; }
		public string BackupDbDirectory { get; set; }

		protected override void UpdateMemberVariables(PersistentXml persistentXml)
        {
			StoreFullName = ((Config) persistentXml).StoreFullName;
			StoreName = ((Config) persistentXml).StoreName;
			StoreAddressLine1 = ((Config) persistentXml).StoreAddressLine1;
			StoreAddressLine2 = ((Config) persistentXml).StoreAddressLine2;
			StorePhoneNumber = ((Config) persistentXml).StorePhoneNumber;
			PrinterName = ((Config) persistentXml).PrinterName;
			BarcodeScannerPortName = ((Config) persistentXml).BarcodeScannerPortName;
			ReportDirectory = ((Config) persistentXml).ReportDirectory;
			BackupDbDirectory = ((Config) persistentXml).BackupDbDirectory;
        }
    }
}
