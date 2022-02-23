using IndyPOS.Mqtt.Events;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Prism.Events;
using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.Mqtt
{
    public class MqttClient : IMqttClient
	{
		private readonly IMqttFactory _mqttFactory;
		private readonly IEventAggregator _eventAggregator;
		private IManagedMqttClient _managedMqttClient;

        public MqttClient(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;
			_mqttFactory = new MqttFactory();
		}

		public async Task Start()
		{
			var options = CreateMqttClientOptions();

			_managedMqttClient = _mqttFactory.CreateManagedMqttClient();
			_managedMqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
			_managedMqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);

			await _managedMqttClient.StartAsync(new ManagedMqttClientOptions { ClientOptions = options });
		}

		private MqttClientOptions CreateMqttClientOptions()
        {
			var clientId = ConfigurationManager.AppSettings.Get("MqttCloudClientId");
			var credentials = CreateMqttClientCredentials();
			var tcpOptions = CreateMqttClientTcpOptions();
			
			var options = new MqttClientOptions
						  {
							  ClientId = clientId,
							  ProtocolVersion = MqttProtocolVersion.V500,
							  ChannelOptions = tcpOptions
						  };

			if (options.ChannelOptions == null)
			{
				throw new InvalidOperationException();
			}

			options.Credentials = credentials;
			options.CleanSession = true;
			options.KeepAlivePeriod = TimeSpan.FromSeconds(60);

			return options;
		}

		private MqttClientCredentials CreateMqttClientCredentials()
        {
			var username = ConfigurationManager.AppSettings.Get("MqttCloudUsername");
			var secret = ConfigurationManager.AppSettings.Get("MqttCloudSecret");

			return new MqttClientCredentials
				   {
					   Username = username,
					   Password = Encoding.UTF8.GetBytes(secret)
				   };
        }

		private MqttClientTcpOptions CreateMqttClientTcpOptions()
        {
			var port = 8883;
			var hostname = ConfigurationManager.AppSettings.Get("MqttCloudHostname");

			var tlsOptions = new MqttClientTlsOptions { UseTls = true };

			if (int.TryParse(ConfigurationManager.AppSettings.Get("MqttCloudPort"), out var portNumber))
			{
				port = portNumber;
			}

			return new MqttClientTcpOptions
				   {
					   Server = hostname,
					   Port = port,
					   TlsOptions = tlsOptions
				   };
		}

		public async Task Stop()
        {
			if (_managedMqttClient == null)
				return;

			await _managedMqttClient.StopAsync();
        }

		public async Task Publish(string topic, string message)
		{
			if (_managedMqttClient == null)
				return;

			try
			{
				var payload = Encoding.UTF8.GetBytes(message);
				var mqttMessage = new MqttApplicationMessageBuilder().WithTopic(topic)
																	 .WithPayload(payload)
																	 .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
																	 .WithRetainFlag()
																	 .Build();

				await _managedMqttClient.PublishAsync(mqttMessage);

				_eventAggregator.GetEvent<MqttMessagePublishedEvent>().Publish();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Publisher connected! " + ex.Message);
				throw;
			}
        }
		
		private void OnPublisherConnected(MqttClientConnectedEventArgs x)
		{
			_eventAggregator.GetEvent<MqttClientConnectedEvent>().Publish();

			Console.WriteLine("Publisher connected!");
		}
		
		private void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
		{
			_eventAggregator.GetEvent<MqttClientDisconnectedEvent>().Publish();

			Console.WriteLine("Publisher disconnected!");
		}
    }
}
