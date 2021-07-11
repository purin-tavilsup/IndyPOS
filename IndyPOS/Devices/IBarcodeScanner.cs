using System;

namespace IndyPOS.Devices
{
	public interface IBarcodeScanner : IDisposable
	{
		void Connect();

		void Disconnect();
	}
}