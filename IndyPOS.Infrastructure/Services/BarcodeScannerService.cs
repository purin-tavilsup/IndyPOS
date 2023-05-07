#nullable enable
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Infrastructure.Services.RawDeviceInput;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Runtime.Versioning;
using System.Text;

namespace IndyPOS.Infrastructure.Services;

[type: SupportedOSPlatform("windows")]
public class BarcodeScannerService : IBarcodeScannerService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreConfigurationService _storeConfigurationService;
	private readonly ILogger<BarcodeScannerService> _logger;
	private RawInput? _rawInput;
	private StringBuilder _buffer;
	private byte[] _keyState;

	private const string ScannerName =
		"\\\\?\\HID#{00001812-0000-1000-8000-00805f9b34fb}_Dev_VID&021915_PID&eeee_REV&0001_eef63a9765ee&Col01#9&1f8abaaf&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}";

	public BarcodeScannerService(IEventAggregator eventAggregator, 
								 IStoreConfigurationService storeConfigurationService, 
								 ILogger<BarcodeScannerService> logger)
	{
		_eventAggregator = eventAggregator;
		_storeConfigurationService = storeConfigurationService;
		_logger = logger;
		_buffer = new StringBuilder();
		_keyState = new byte[256];
	}

	public void Start(IntPtr handle)
	{
		Stop();

		_rawInput = new RawInput(handle, captureOnlyInForeground: true);

		//_rawInput.AddMessageFilter(); // Adding a message filter will cause keypresses to be handled
		//Win32.DeviceAudit();          // Writes a file DeviceAudit.txt to the current directory

		_buffer = new StringBuilder();
		_keyState = new byte[256];

		_rawInput.KeyPressed += OnKeyPressed;
	}

	public void Stop()
	{
		if (_rawInput is null)
			return;

		_rawInput.KeyPressed -= OnKeyPressed;
		_rawInput = null;
	}

	private void AppendCharacter(ushort virtualKey, ref StringBuilder output, ref byte[] keyState)
	{
		if (Win32.MapVirtualKeyToCharacter(virtualKey) == 0)
		{
			keyState[virtualKey] = 0x80;

			return;
		}

		var buffer = new StringBuilder(2);
		
		var numberOfCharacters = Win32.TranslateVirtualKeyToAscii(virtualKey, keyState, buffer);

		if (numberOfCharacters > 0)
		{
			var characters = buffer.ToString(0, numberOfCharacters);

			output.Append(characters);
		}

		keyState = new byte[256];
	}

    private void OnKeyPressed(object sender, RawInputEventArg e)
	{
		//TODO: Refactor to supports multiple scanners 

		// Handle only key press events from a specific scanner
		if (e.KeyPressEvent.DeviceName != ScannerName || 
			e.KeyPressEvent.KeyPressState == "MAKE")
		{
			return;
		}

		// All keys except "ENTER" will be translated to characters and stored in buffer 
		if (e.KeyPressEvent.VKeyName != "ENTER")
		{
			AppendCharacter((ushort)e.KeyPressEvent.VKey, ref _buffer, ref _keyState );

			return;
		}

		if (_buffer.Length > 0)
		{
			_logger.LogInformation($"Output: {_buffer}");

			_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(_buffer.ToString());

			_buffer.Clear();
			_keyState = new byte[256];
		}
	}
}