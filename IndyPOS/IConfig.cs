namespace IndyPOS
{
    public interface IConfig
    {
		string StoreFullName { get; set; }
		string StoreName { get; set; }
		string StoreAddressLine1 { get; set; }
		string StoreAddressLine2 { get; set; }
		string StorePhoneNumber { get; set; }
		string PrinterName { get; set; }
		string BarcodeScannerPortName { get; set; }
		string ReportDirectory { get; set; }
		string TransactionDirectory { get; set; }

		string FileName { get; set; }
		void Save();
		bool Load();
	}
}
