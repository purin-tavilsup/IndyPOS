using System.Threading.Tasks;

namespace IndyPOS.Mqtt
{
    public interface IMqttClient
	{
		Task Start();

		Task Stop();

		Task Publish(string topic, string message);
	}
}
