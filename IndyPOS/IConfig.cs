namespace IndyPOS
{
    public interface IConfig
    {
		string StoreName { get; set; }
		string StoreAddressLine1 { get; set; }
		string StoreAddressLine2 { get; set; }
		string StorePhoneNumber { get; set; }
		string StoreTaxId { get; set; }
		string PrinterName { get; set; }

		string FileName { get; set; }
		void Save();
		bool Load();
	}
}
