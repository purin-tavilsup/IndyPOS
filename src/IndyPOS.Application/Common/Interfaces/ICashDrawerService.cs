namespace IndyPOS.Application.Common.Interfaces;

public interface ICashDrawerService
{
	void Configure(string serialPortName, int code);

	void OpenCashDrawer();
}