#nullable enable
using System.IO.Ports;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace IndyPOS.Infrastructure.Services;

public class BarcodeScannerService : IBarcodeScannerService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IStoreConfigurationService _storeConfigurationService;
	private readonly ILogger<BarcodeScannerService> _logger;
	private SerialPort? _serialPort;
	
	public BarcodeScannerService(IEventAggregator eventAggregator, 
								IStoreConfigurationService storeConfigurationService, 
								ILogger<BarcodeScannerService> logger)
	{
		_eventAggregator = eventAggregator;
		_storeConfigurationService = storeConfigurationService;
		_logger = logger;
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