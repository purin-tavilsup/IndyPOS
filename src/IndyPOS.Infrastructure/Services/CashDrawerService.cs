using System.IO.Ports;
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

[type:SupportedOSPlatform("windows")]
public class CashDrawerService : ICashDrawerService
{
	private readonly SerialPort _serialPort;
	private string _serialPortName = "COM1";
	private int _drawerCode = 1;
	private readonly ILogger<CashDrawerService> _logger;
	
	public CashDrawerService(IStoreConfigurationService storeConfigurationService, ILogger<CashDrawerService> logger)
	{
		_logger = logger;
		_serialPort = new SerialPort();
		
		GetStoreConfiguration(storeConfigurationService);
		InitializeSerialPort();
	}

	~CashDrawerService()
	{
		if (!_serialPort.IsOpen)
		{
			return;
		}
		
		_serialPort.Close();
		_serialPort.Dispose();
	}

	public void Configure(string serialPortName, int code)
	{
		if (_serialPort.IsOpen)
		{
			_serialPort.Close();
		}

		_serialPortName = serialPortName;
		_drawerCode = code;

		InitializeSerialPort();
	}

	private void InitializeSerialPort()
	{
		_serialPort.PortName = _serialPortName;
		_serialPort.BaudRate = 9600;
		_serialPort.DataBits = 8;
		_serialPort.Parity = Parity.None;
		_serialPort.StopBits = StopBits.One;
		_serialPort.Handshake = Handshake.None;

		// A cash drawer is optional hardware. If the configured COM port is
		// absent (e.g. no drawer attached), opening it must not bring the app
		// down — log and carry on; OpenCashDrawer() guards on IsOpen.
		try
		{
			_serialPort.Open();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to open cash drawer serial port '{PortName}'. The cash drawer will be unavailable.", _serialPortName);
		}
	}
	
	private void GetStoreConfiguration(IStoreConfigurationService storeConfigurationService)
	{
		try
		{
			var config = storeConfigurationService.Get();

			_serialPortName = config.SerialPortName ?? "COM1";
			_drawerCode = config.Code ?? 1;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to get store configuration for a receipt printer!");
			throw;
		}
	}
	
	public void OpenCashDrawer()
	{
		if (!_serialPort.IsOpen)
		{
			_logger.LogWarning("Cash drawer serial port '{PortName}' is not open; ignoring open-drawer request.", _serialPortName);
			return;
		}

		var command = GetCommand(_drawerCode);

		_serialPort.Write(command, 0, command.Length);
	}

	private static byte[] GetCommand(int code)
	{
		// ESC-POS command: ESC p m t1 t2

		return code switch
		{
			// 27, 112, 0, 25, 250
			1 => [0x1B, 0x70, 0x00, 0x19, 0xFA],
			
			// 27, 112, 0, 148, 49
			2 => [0x1B, 0x70, 0x00, 0x94, 0x31],
			
			// 27, 112, 0, 50, 250
			3 => [0x1B, 0x70, 0x00, 0x32, 0xFA],
			
			// 27, 112, 0, 25, 250
			_ => [0x1B, 0x70, 0x00, 0x19, 0xFA]
		};
	}
}