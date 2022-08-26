using IndyPOS.Common.Interfaces;
using IndyPOS.Events;
using IndyPOS.Interfaces;
using Prism.Events;
using Serilog;
using System;
using System.IO.Ports;

namespace IndyPOS.Devices
{
	public class BarcodeScanner : IBarcodeScanner
    {
        private readonly IEventAggregator _eventAggregator;
		private readonly IConfiguration _configuration;
		private readonly ILogger _logger;

        private SerialPort _serialPort;

        public BarcodeScanner(IEventAggregator eventAggregator,
							  IConfiguration configuration,
							  ILogger logger)
		{
            _eventAggregator = eventAggregator;
			_configuration = configuration;
			_logger = logger;
		}

        public void Connect()
		{
            try
			{
				_serialPort = new SerialPort
				{
					PortName = _configuration.BarcodeScannerPortName,
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
				_logger.Error(ex, $"Failed to connect to Barcode Scanner on port {_serialPort.PortName}.");
			}
		}

		private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			var data = _serialPort.ReadTo("\r");

			_eventAggregator.GetEvent<BarcodeReceivedEvent>().Publish(data);
		}

		public void Disconnect()
		{
			if (_serialPort == null)
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
}
