using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace IndyPOS.Infrastructure.Services.RawDeviceInput
{
    [type: SupportedOSPlatform("windows")]
	public sealed class RawKeyboard
	{
		public delegate void DeviceEventHandler(object sender, RawInputEventArg e);
		public event DeviceEventHandler? KeyPressed;
		private static InputData _rawBuffer;
		private readonly object _padLock = new();
		private readonly Dictionary<IntPtr,KeyPressEvent> _deviceList = new();
		private readonly string _barcodeScannerDeviceName;

		public int NumberOfKeyboards { get; private set; }
		
		public RawKeyboard(IntPtr handle, bool captureOnlyInForeground, string barcodeScannerDeviceName)
		{
			var rawInputDevice = new RawInputDevice[1];

			rawInputDevice[0].UsagePage = HidUsagePage.GENERIC;       
			rawInputDevice[0].Usage = HidUsage.Keyboard;              
            rawInputDevice[0].Flags = (captureOnlyInForeground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY;
			rawInputDevice[0].Target = handle;

			_barcodeScannerDeviceName = barcodeScannerDeviceName;

			if(!Win32.RegisterRawInputDevices(rawInputDevice, (uint)rawInputDevice.Length, (uint)Marshal.SizeOf(rawInputDevice[0])))
			{
				throw new ApplicationException("Failed to register raw input device(s).");
			}
		}

		public void EnumerateDevices()
		{
			lock (_padLock)
			{
				_deviceList.Clear();

				var keyboardNumber = 0;
				var numberOfDevices = 0;
				uint deviceCount = 0;
				var dwSize = (Marshal.SizeOf(typeof(Rawinputdevicelist)));

				if (Win32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
				{
					var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                    _ = Win32.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

					for (var i = 0; i < deviceCount; i++)
					{
						uint pcbSize = 0;

						// On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
						var rid = (Rawinputdevicelist)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(Rawinputdevicelist));

                        _ = Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

						if (pcbSize <= 0) { continue; }

						var pData = Marshal.AllocHGlobal((int)pcbSize);

                        _ = Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, pData, ref pcbSize);

						var deviceName = Marshal.PtrToStringAnsi(pData) ?? string.Empty;

                        if (rid.dwType is DeviceType.RimTypekeyboard or DeviceType.RimTypeHid)
						{
							var deviceDesc = Win32.GetDeviceDescription(deviceName);

							var dInfo = new KeyPressEvent
							{
								DeviceName = Marshal.PtrToStringAnsi(pData) ?? string.Empty,
								DeviceHandle = rid.hDevice,
								DeviceType = Win32.GetDeviceType(rid.dwType),
								Name = deviceDesc,
								Source = keyboardNumber++.ToString(CultureInfo.InvariantCulture)
							};
						   
							if (!_deviceList.ContainsKey(rid.hDevice))
							{
								numberOfDevices++;

								_deviceList.Add(rid.hDevice, dInfo);
							}
						}

						Marshal.FreeHGlobal(pData);
					}

					Marshal.FreeHGlobal(pRawInputDeviceList);

					NumberOfKeyboards = numberOfDevices;
					Debug.WriteLine("EnumerateDevices() found {0} Keyboard(s)", NumberOfKeyboards);

					return;
				}
			}
			
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	   
		public void ProcessRawInput(IntPtr hdevice)
		{
			if (_deviceList.Count == 0) return;

			var dwSize = 0;

            _ = Win32.GetRawInputData(hdevice, DataCommand.RID_INPUT, IntPtr.Zero, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader)));

			if (dwSize != Win32.GetRawInputData(hdevice, DataCommand.RID_INPUT, out _rawBuffer, ref dwSize, Marshal.SizeOf(typeof (Rawinputheader))))
			{
				Debug.WriteLine("Error getting the rawinput buffer");
				return;
			}

			int virtualKey = _rawBuffer.data.keyboard.VKey;
			int flags = _rawBuffer.data.keyboard.Flags;

			if (virtualKey == Win32.KEYBOARD_OVERRUN_MAKE_CODE) return;

			KeyPressEvent keyPressEvent;

			if (_deviceList.ContainsKey(_rawBuffer.header.hDevice))
			{
				lock (_padLock)
				{
					keyPressEvent = _deviceList[_rawBuffer.header.hDevice];
				}
			}
			else
			{
				Debug.WriteLine("Handle: {0} was not in the device list.", _rawBuffer.header.hDevice);
				return;
			}

			var isBreakBitSet = ((flags & Win32.RI_KEY_BREAK) != 0);
			
			keyPressEvent.KeyPressState = isBreakBitSet ? "BREAK" : "MAKE"; 
			keyPressEvent.Message = _rawBuffer.data.keyboard.Message;
			keyPressEvent.VKeyName = KeyMapper.GetMicrosoftKeyName(virtualKey).ToUpper();
			keyPressEvent.VKey = virtualKey;

			KeyPressed?.Invoke(this, new RawInputEventArg(keyPressEvent));
		}
	}
}
