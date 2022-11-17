namespace IndyPOS.Facade.Interfaces;

public interface IBarcodeScannerHelper : IDisposable
{
	void Connect();

	void Disconnect();
}