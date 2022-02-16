using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.Mqtt
{
    public class MqttClient : IMqttClient
	{
		private readonly IMqttFactory _mqttFactory;
		private IManagedMqttClient _managedMqttClient;
		private const string ClientId = "rungratpos1";

        public MqttClient()
        {
			_mqttFactory = new MqttFactory();
		}

		public async Task Start()
		{
			const string hostname = "3fef886d89044ebdb2add9ec32c937a9.s1.eu.hivemq.cloud";
			const int port = 8883;
			const string username = "rungratpos";
			const string secret = "myHiveMQ101";

			var credentials = new MqttClientCredentials
							  {
								  Username = username,
								  Password = Encoding.UTF8.GetBytes(secret)
							  };

			var tlsOptions = new MqttClientTlsOptions { UseTls = true };

			var tcpOptions = new MqttClientTcpOptions
							 {
								 Server = hostname,
								 Port = port,
								 TlsOptions = tlsOptions
							 };
			
			var options = new MqttClientOptions
						  {
							  ClientId = ClientId,
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

			_managedMqttClient = _mqttFactory.CreateManagedMqttClient();
			_managedMqttClient.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
			_managedMqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
			_managedMqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);

			await _managedMqttClient.StartAsync(new ManagedMqttClientOptions { ClientOptions = options });
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
			}
			catch (Exception ex)
			{
				Console.WriteLine("Publisher connected! " + ex.Message);
			}
        }

		/// <summary>
		/// Handles the publisher connected event.
		/// </summary>
		/// <param name="x">The MQTT client connected event args.</param>
		private static void OnPublisherConnected(MqttClientConnectedEventArgs x)
		{
			Console.WriteLine("Publisher connected!");
		}

		/// <summary>
		/// Handles the publisher disconnected event.
		/// </summary>
		/// <param name="x">The MQTT client disconnected event args.</param>
		private static void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
		{
			Console.WriteLine("Publisher disconnected!");
		}

		/// <summary>
		/// Handles the received application message event.
		/// </summary>
		/// <param name="x">The MQTT application message received event args.</param>
		private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
		{
			//var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
			//this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
		}
    }
}
