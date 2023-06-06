using IndyPOS.Application.Common.Enums;

namespace IndyPOS.Application.Common.Interfaces;

public interface IRawInputDeviceService
{
	void Start(IntPtr handle);

	void Stop();

	void SetMode(RawInputDeviceMode mode);

	void LoadConfiguration();
}