namespace IndyPOS.Application.Common.Interfaces;

public interface IBarcodeScannerService
{
	void Start(IntPtr handle);

	void Stop();
}