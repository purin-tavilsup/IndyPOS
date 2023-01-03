namespace IndyPOS.Application.Interfaces;

public interface IBarcodeScannerHelper : IDisposable
{
	void Connect();

	void Disconnect();
}