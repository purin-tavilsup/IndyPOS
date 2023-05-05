#nullable enable
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Infrastructure.Services.RawDeviceInput;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Text;

namespace IndyPOS.Infrastructure.Services;

[type: SupportedOSPlatform("windows")]
public class BarcodeScannerService : IBarcodeScannerService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreConfigurationService _storeConfigurationService;
	private readonly ILogger<BarcodeScannerService> _logger;
	private SerialPort? _serialPort;
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
		}
		else
		{
			var buffer = new StringBuilder(2);

			var numberOfCharacters = Win32.TranslateVirtualKeyToAscii(virtualKey, keyState, buffer);

			if (numberOfCharacters > 0)
			{
				var characters = buffer.ToString(0, numberOfCharacters);

				_logger.LogInformation($"Character: {characters}");

				output.Append(characters);
			}

			keyState = new byte[256];
		}
	}

    private void OnKeyPressed(object sender, RawInputEventArg e)
	{
		if (e.KeyPressEvent.DeviceName != ScannerName)
		{
			return;
		}

		if (e.KeyPressEvent.KeyPressState == "BREAK")
		{
			return;
		}

		if (e.KeyPressEvent.VKeyName == "ENTER" && _buffer.Length > 0)
		{
			_logger.LogInformation($"Output: {_buffer}");

			_buffer.Clear();
			_keyState = new byte[256];
		}
		else
		{
			_logger.LogInformation($"Code: {e.KeyPressEvent.VKey.ToString(CultureInfo.InvariantCulture)}");

			AppendCharacter((ushort)e.KeyPressEvent.VKey, ref _buffer, ref _keyState );
		}
	}

    public void Connect()
	{
		try
		{
			var config = _storeConfigurationService.Get();
			var portName = config.BarcodeScannerPortName ?? "COM4";

			_serialPort = new SerialPort
			{
				PortName = portName,
				BaudRate = 115200,
				DataBits = 8,
				StopBits = StopBits.One,
				Parity = Parity.None
			};

			_serialPort.Open();
			_serialPort.DataReceived += SerialPort_DataReceived;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, $"Failed to connect to Barcode Scanner on port {_serialPort?.PortName}.");
		}
	}

	private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
	{
		if (_serialPort is null)
			return;

		var data = _serialPort.ReadTo("\r");

		_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(data);
	}

	public void Disconnect()
	{
		if (_serialPort is null)
			return;

		try
		{
			_serialPort.DataReceived -= SerialPort_DataReceived;

			if (_serialPort.IsOpen)
				_serialPort.Close();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, $"Failed to disconnect Barcode Scanner on port {_serialPort.PortName}.");
		}
	}

	public void Dispose()
	{
		Disconnect();
	}
}