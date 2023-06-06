#nullable enable
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Infrastructure.Services.RawDeviceInput;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Runtime.Versioning;
using System.Text;

namespace IndyPOS.Infrastructure.Services;

[type: SupportedOSPlatform("windows")]
public class RawInputDeviceService : IRawInputDeviceService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreConfigurationService _storeConfigurationService;
	private readonly ILogger<RawInputDeviceService> _logger;
	private RawInputDeviceMode _mode;
	private RawInput? _rawInput;
	private StringBuilder _buffer;
	private byte[] _keyState;
	private string _barcodeScannerDeviceName = string.Empty;

	public RawInputDeviceService(IEventAggregator eventAggregator, 
								 IStoreConfigurationService storeConfigurationService, 
								 ILogger<RawInputDeviceService> logger)
	{
		_eventAggregator = eventAggregator;
		_storeConfigurationService = storeConfigurationService;
		_logger = logger;
		_buffer = new StringBuilder();
		_keyState = new byte[256];
		_mode = RawInputDeviceMode.GetInputValue;
	}

	public void Start(IntPtr handle)
	{
		Stop();
		LoadConfiguration();

		_rawInput = new RawInput(handle, captureOnlyInForeground: true, _barcodeScannerDeviceName);
		_buffer = new StringBuilder();
		_keyState = new byte[256];

		_rawInput.KeyPressed += OnKeyPressed;

		_logger.LogInformation("Raw input device service started");
	}

	public void LoadConfiguration()
	{
		var config = _storeConfigurationService.Get();

		_barcodeScannerDeviceName = config.BarcodeScannerDeviceName ?? string.Empty;
	}

	public void Stop()
	{
		if (_rawInput is null)
			return;

		_rawInput.KeyPressed -= OnKeyPressed;
		_rawInput = null;

		_logger.LogInformation("Raw input device service stopped");
	}

	public void SetMode(RawInputDeviceMode mode)
	{
		_mode = mode;
	}

    private void OnKeyPressed(object sender, RawInputEventArg e)
	{
		// RawInput will always produce 2 events for each key.
		// One for KeyPressState = "MAKE" and the other for KeyPressState = "BREAK".
		// The first event can be skipped.
		if (e.KeyPressEvent.KeyPressState == "MAKE")
		{
			return;
		}

		if (_mode == RawInputDeviceMode.GetInputValue)
		{
			ProcessRawInput(e.KeyPressEvent, ref _keyState);

			return;
		}

		if (_mode == RawInputDeviceMode.GetDeviceName)
		{
			ProcessDeviceName(e.KeyPressEvent);
		}
	}

	private void ProcessRawInput(KeyPressEvent keyPressEvent, ref byte[] keyState)
	{
		if (keyPressEvent.DeviceName == _barcodeScannerDeviceName)
		{
			ProcessScannerInput(keyPressEvent, ref keyState);
		}
	}

	private void ProcessScannerInput(KeyPressEvent keyPressEvent, ref byte[] keyState)
	{
		var virtualKey = (ushort) keyPressEvent.VKey;

		// All keys except "ENTER" will be translated to characters and stored in buffer 
		if (keyPressEvent.VKeyName != "ENTER")
		{
			// Skip keys that have no translation
			if (Win32.MapVirtualKeyToCharacter(virtualKey) == 0)
			{
				// Set high-order bit to '1' (0x80 or 1000 0000) for Key Down
				keyState[virtualKey] = 0x80;

				return;
			}

			var buffer = new StringBuilder(2);

			var numberOfCharacters = Win32.TranslateVirtualKeyToUnicode(virtualKey, keyState, buffer);

			if (numberOfCharacters > 0)
			{
				var characters = buffer.ToString(0, numberOfCharacters);

				_buffer.Append(characters);
			}

			// Reset
			keyState = new byte[256];

			return;
		}

		if (keyPressEvent.VKeyName == "ENTER" && _buffer.Length > 0)
		{
			_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(_buffer.ToString());

			_buffer.Clear();
			_keyState = new byte[256];
		}
	}

	private void ProcessDeviceName(KeyPressEvent keyPressEvent)
	{
		// Only the last key ("ENTER") is needed for getting device name
		if (keyPressEvent.VKeyName != "ENTER")
		{
			return;
		}

		var deviceName = keyPressEvent.DeviceName;

		_eventAggregator.GetEvent<RawInputDeviceNameReceivedEvent>().Publish(deviceName);

		// Reset mode back to default
		_mode = RawInputDeviceMode.GetInputValue;
	}
}