using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace IndyPOS.Infrastructure.Services.RawDeviceInput;

//TODO: Refactor and clean up

[type: SupportedOSPlatform("windows")]
public static class Win32
{
	// ReSharper disable InconsistentNaming
	public const int KEYBOARD_OVERRUN_MAKE_CODE = 0xFF;
	private const int FAPPCOMMANDMASK = 0xF000;

	public const int WM_KEYDOWN = 0x0100;
	internal const int WM_INPUT = 0x00FF;
	internal const int WM_USB_DEVICECHANGE = 0x0219;
	internal const int VK_SHIFT = 0x10;

	internal const int RI_KEY_MAKE = 0x00;  // Key Down
	internal const int RI_KEY_BREAK = 0x01; // Key Up
	internal const int RI_KEY_E0 = 0x02;    // Left version of the key
	internal const int RI_KEY_E1 = 0x04;    // Right version of the key. Only seems to be set for the Pause/Break key.
        
	internal const int VK_CONTROL = 0x11;
	internal const int VK_MENU = 0x12;
	internal const int VK_ZOOM = 0xFB;
	internal const int VK_LSHIFT = 0xA0;
	internal const int VK_RSHIFT = 0xA1;
	internal const int VK_LCONTROL = 0xA2;
	internal const int VK_RCONTROL = 0xA3;
	internal const int VK_LMENU = 0xA4;
	internal const int VK_RMENU = 0xA5;
        
	internal const int SC_SHIFT_R = 0x36;     
	internal const int SC_SHIFT_L = 0x2a;   
	internal const int RIM_INPUT = 0x00;

	private const int MAPVK_VK_TO_CHAR = 2;
	private const nint US_KEYBOARD_ID = 67699721;
	
	[DllImport("User32.dll", SetLastError = true)]
	internal static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] out InputData buffer, [In, Out] ref int size, int cbSizeHeader);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] IntPtr pData, [In, Out] ref int size, int sizeHeader);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputDeviceInfo command, IntPtr pData, ref uint size);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint numberDevices, uint size);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevice, uint numberDevices, uint size);
        
	[DllImport("user32.dll", SetLastError = true)]
	internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, DeviceNotification flags);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool UnregisterDeviceNotification(IntPtr handle);

	[DllImport("user32.dll")]
	private static extern int MapVirtualKey(uint uCode, uint uMapType);

	[DllImport("user32.dll")]
	private static extern int ToUnicodeEx(uint virtualKeyCode, 
										  uint scanCode, 
										  byte[] keyboardState, 
										  [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, 
										  int bufferSize, 
										  uint flags,
										  [Out] IntPtr inputLocaleId);

	[DllImport("Kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout(uint idThread);

	public static string GetDeviceType(uint device)
	{
        var deviceType = device switch
        {
            DeviceType.RimTypemouse => "MOUSE",
            DeviceType.RimTypekeyboard => "KEYBOARD",
            DeviceType.RimTypeHid => "HID",
            _ => "UNKNOWN",
        };

        return deviceType;
	}

	public static string GetDeviceDescription(string device)
	{
		string deviceDesc;

		try
		{
			var deviceKey = RegistryAccess.GetDeviceKey(device);
			deviceDesc = deviceKey?.GetValue("DeviceDesc")?.ToString() ?? string.Empty;
			deviceDesc = deviceDesc[(deviceDesc.IndexOf(';') + 1)..];
		}
		catch (Exception)
		{
			deviceDesc = "Device is malformed unable to look up in the registry";
		}

		return deviceDesc;
	}

	/// <summary>
	/// Maps a virtual-key code to a character value.
	/// If there is no translation, the function returns 0.
	/// </summary>
	/// <param name="virtualKey"></param>
	/// <returns></returns>
	public static int MapVirtualKeyToCharacter(uint virtualKey)
	{
		return MapVirtualKey(virtualKey, MAPVK_VK_TO_CHAR);
	}

	/// <summary>
	/// Translates the specified virtual-key code and keyboard state to the corresponding Unicode character or characters.
	///  The function translates the code using US keyboard layout.
	/// </summary>
	/// <param name="virtualKey"></param>
	/// <param name="keyState"></param>
	/// <param name="characters"></param>
	/// <returns></returns>
	public static int TranslateVirtualKeyToUnicode(uint virtualKey, byte[] keyState, StringBuilder characters)
	{
		return ToUnicodeEx(virtualKey, 0, keyState, characters, characters.Capacity, 0, US_KEYBOARD_ID);
	}

	/// <summary>
	/// Gets input locale identifier
	/// </summary>
	/// <returns></returns>
	public static IntPtr GetInputLanguageId()
	{
		var threadId = GetCurrentThreadId();

		return GetKeyboardLayout(threadId);
	}
}