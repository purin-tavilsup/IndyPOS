using IndyPOS.Devices;
using IndyPOS.UI;

namespace IndyPOS
{
	public class Machine : IMachine
    {
        private readonly MainForm _mainForm;
        private readonly IBarcodeScanner _barcodeScanner;

        public Machine(MainForm mainForm, IBarcodeScanner barcodeScanner)
        {
            _mainForm = mainForm;
            _barcodeScanner = barcodeScanner;
        }

		public void Dispose()
		{
            Shutdown();
		}

		public void Launch()
        {
            ConnectDevices();
            StartUI();
        }

        public void Shutdown()
		{
            DisconnectDevices();
        }

        private void ConnectDevices()
		{
            _barcodeScanner.Connect();
        }

        private void DisconnectDevices()
		{
            _barcodeScanner?.Disconnect();
		}

        private void StartUI()
        {
            System.Windows.Forms.Application.Run(_mainForm);
        }
    }
}
