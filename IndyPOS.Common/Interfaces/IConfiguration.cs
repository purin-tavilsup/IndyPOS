using System.Threading.Tasks;

namespace IndyPOS.Common.Interfaces
{
    public interface IConfiguration
    {
        string ConfigDirectory { get; }

        string DatabasePath { get; }

        string ReportsDirectory { get; }

		string LogDirectory { get; }

		string DataFeedBaseUri { get; }

		string StoreFullName { get; set; }

		string StoreName { get; set; }

		string StoreAddressLine1 { get; set; }

		string StoreAddressLine2 { get; set; }

		string StorePhoneNumber { get; set; }

		string PrinterName { get; set; }

		string BarcodeScannerPortName { get; set; }

		string BackupDbDirectory { get; set; }

		string BarcodeDirectory { get; set; }

		string DataFeedKey { get; set; }

		bool DataFeedEnabled { get; set; }

		Task UpdateAsync();
	}
}
