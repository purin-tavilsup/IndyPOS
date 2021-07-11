﻿using IndyPOS.Events;
using Prism.Events;
using System.IO.Ports;

namespace IndyPOS.Devices
{
	public class BarcodeScanner : IBarcodeScanner
    {
        private readonly IEventAggregator _eventAggregator;

        private SerialPort _serialPort;

        public BarcodeScanner(IEventAggregator eventAggregator)
		{
            _eventAggregator = eventAggregator;
		}

        public void Connect()
		{
            try
			{
				_serialPort = new SerialPort
				{
					PortName = "COM3",
					BaudRate = 115200,
					DataBits = 8,
					StopBits = StopBits.One,
					Parity = Parity.None
				};

				_serialPort.Open();
				_serialPort.DataReceived += SerialPort_DataReceived;
			}
			catch
			{
				// Log error here
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
			catch
			{
				// Log error here
			}
		}

		public void Dispose()
		{
			Disconnect();
		}
	}
}
