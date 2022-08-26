using System;

namespace IndyPOS.Interfaces
{
	public interface IBarcodeScanner : IDisposable
	{
		void Connect();

		void Disconnect();
	}
}