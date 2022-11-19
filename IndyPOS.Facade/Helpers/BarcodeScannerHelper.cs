#nullable enable
using IndyPOS.Facade.Events;
using IndyPOS.Facade.Interfaces;
using Prism.Events;
using Serilog;
using System.IO.Ports;

namespace IndyPOS.Facade.Helpers;

public class BarcodeScannerHelper : IBarcodeScannerHelper
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreConfigurationHelper _storeConfigurationHelper;
	private readonly ILogger _logger;
	private SerialPort? _serialPort;
		
	public BarcodeScannerHelper(IEventAggregator eventAggregator, 
								IStoreConfigurationHelper storeConfigurationHelper, 
								ILogger logger)
	{
		_eventAggregator = eventAggregator;
		_storeConfigurationHelper = storeConfigurationHelper;
		_logger = logger;
	}

	public void Connect()
	{
		try
		{
			var config = _storeConfigurationHelper.Get();
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
			_logger.Error(ex, $"Failed to connect to Barcode Scanner on port {_serialPort?.PortName}.");
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
			_logger.Error(ex, $"Failed to disconnect Barcode Scanner on port {_serialPort.PortName}.");
		}
	}

	public void Dispose()
	{
		Disconnect();
	}
}