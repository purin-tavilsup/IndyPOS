namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeScannerService : IDisposable
{
	void Connect();

	void Disconnect();
}