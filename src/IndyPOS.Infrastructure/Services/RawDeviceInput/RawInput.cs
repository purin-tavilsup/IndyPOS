using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace IndyPOS.Infrastructure.Services.RawDeviceInput;

[type: SupportedOSPlatform("windows")]
public class RawInput : NativeWindow  
{
	private static RawKeyboard _keyboardDriver;
	private readonly IntPtr _devNotifyHandle;
	private static readonly Guid DeviceInterfaceHid = new("4D1E55B2-F16F-11CF-88CB-001111000030");
	private PreMessageFilter? _filter;

	public event RawKeyboard.DeviceEventHandler KeyPressed
	{
		add => _keyboardDriver.KeyPressed += value;
		remove => _keyboardDriver.KeyPressed -= value;
	}

	public int NumberOfKeyboards => _keyboardDriver.NumberOfKeyboards;

	public void AddMessageFilter()
	{
		if (_filter is not null) return;
            
		_filter = new PreMessageFilter();
		System.Windows.Forms.Application.AddMessageFilter(_filter);
	}

	private void RemoveMessageFilter()
	{
		if (_filter is null) return;

		System.Windows.Forms.Application.RemoveMessageFilter(_filter);
	}

	public RawInput(IntPtr parentHandle, bool captureOnlyInForeground)
	{
		AssignHandle(parentHandle);

		_keyboardDriver = new RawKeyboard(parentHandle, captureOnlyInForeground);
		_keyboardDriver.EnumerateDevices();

		_devNotifyHandle = RegisterForDeviceNotifications(parentHandle);
	}

	private static IntPtr RegisterForDeviceNotifications(IntPtr parent)
	{
		var usbNotifyHandle = IntPtr.Zero;
		var bdi = new BroadcastDeviceInterface();
		bdi.DbccSize = Marshal.SizeOf(bdi);
		bdi.BroadcastDeviceType = BroadcastDeviceType.DBT_DEVTYP_DEVICEINTERFACE;
		bdi.DbccClassguid = DeviceInterfaceHid;

		var mem = IntPtr.Zero;
		try
		{
			mem = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BroadcastDeviceInterface)));
			Marshal.StructureToPtr(bdi, mem, false);
			usbNotifyHandle = Win32.RegisterDeviceNotification(parent, mem, DeviceNotification.DEVICE_NOTIFY_WINDOW_HANDLE);
		}
		catch (Exception e)
		{
			Debug.Print("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
			Debug.Print(e.StackTrace);
		}
		finally
		{
			Marshal.FreeHGlobal(mem);
		}

		if (usbNotifyHandle == IntPtr.Zero)
		{
			Debug.Print("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
		}
            
		return usbNotifyHandle;
	}

	protected override void WndProc(ref Message message)
	{
		switch (message.Msg)
		{
			case Win32.WM_INPUT:
			{
				_keyboardDriver.ProcessRawInput(message.LParam);
			}
				break;

			case Win32.WM_USB_DEVICECHANGE:
			{
				Debug.WriteLine("USB Device Arrival / Removal");
				_keyboardDriver.EnumerateDevices();
			}
				break;
		}

		base.WndProc(ref message);
	}
        
	~RawInput()
	{
		Win32.UnregisterDeviceNotification(_devNotifyHandle);
		RemoveMessageFilter();
	}
}