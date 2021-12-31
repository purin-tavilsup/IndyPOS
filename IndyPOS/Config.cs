using IndyPOS.Utilities;

namespace IndyPOS
{
    public class Config : PersistentXml, IConfig
	{
		public string StoreName { get; set; }
		public string StoreAddressLine1 { get; set; }
		public string StoreAddressLine2 { get; set; }
		public string StorePhoneNumber { get; set; }
		public string StoreTaxId { get; set; }

		public string PrinterName { get; set; }

		protected override void UpdateMemberVariables(PersistentXml persistentXml)
        {
			StoreName = ((Config)persistentXml).StoreName;
			StoreAddressLine1 = ((Config)persistentXml).StoreAddressLine1;
			StoreAddressLine2 = ((Config)persistentXml).StoreAddressLine2;
			StorePhoneNumber = ((Config)persistentXml).StorePhoneNumber;
			StoreTaxId = ((Config)persistentXml).StoreTaxId;
			PrinterName = ((Config)persistentXml).PrinterName;
        }
    }
}
