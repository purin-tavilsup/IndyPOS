using Prism.Events;

namespace IndyPOS.Application.Events;

/// <summary>
/// Event for notifying an input device's name has been received.
/// The device name will be passed along with the event.
/// </summary>
public class RawInputDeviceNameReceivedEvent: PubSubEvent<string>
{
}