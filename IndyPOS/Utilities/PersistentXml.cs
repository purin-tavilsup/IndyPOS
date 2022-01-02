using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace IndyPOS.Utilities
{
    public class PersistentXml
    {
		protected PersistentXml()
        {
            FileName = "PersistentXml.config";
        }
        
		[Browsable(false)]
		[ReadOnly(true)]
        public string FileName { get; set; }

		public void Save()
        {
            StreamWriter myWriter = null;

			try
			{
				var mySerializer = new XmlSerializer(GetType());
				myWriter = new StreamWriter(FileName, false);

				mySerializer.Serialize(myWriter, this);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + ex.InnerException);
			}
            finally
			{
				myWriter?.Close();
			}
        }

        public bool Load()
        {
			FileStream myFileStream = null;
            var fileExists = false;

            try
            {
                var mySerializer = new XmlSerializer(GetType());
                var file = new FileInfo(FileName);
                
                if (file.Exists)
                {
                    myFileStream = file.OpenRead();

                    var myAppSettings = (PersistentXml)mySerializer.Deserialize(myFileStream);

                    UpdateMemberVariables(myAppSettings);

                    fileExists = true;
                }
            }
            finally
			{
				myFileStream?.Close();
			}

            return fileExists;
        }

        protected virtual void UpdateMemberVariables(PersistentXml persistentXml)
        {
            foreach (var fromProperty in persistentXml.GetType().GetProperties())
            {
                foreach (var toProperty in this.GetType().GetProperties())
                {
                    // Do not replace special "FileName" property with persisted value
                    if (toProperty.CanWrite && fromProperty.Name == toProperty.Name && toProperty.Name != "FileName")
                    {
                        toProperty.SetValue(this, fromProperty.GetValue(persistentXml, null), null);
                        break;
                    }
                }
            }
        }
    }
}
