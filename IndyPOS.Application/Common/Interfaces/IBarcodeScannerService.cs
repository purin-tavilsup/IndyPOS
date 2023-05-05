namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeScannerService : IDisposable
{
	void Start(IntPtr handle);

	void Stop();

	void Connect();

	void Disconnect();
}