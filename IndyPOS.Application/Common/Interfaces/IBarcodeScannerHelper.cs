namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeScannerHelper : IDisposable
{
	void Connect();

	void Disconnect();
}