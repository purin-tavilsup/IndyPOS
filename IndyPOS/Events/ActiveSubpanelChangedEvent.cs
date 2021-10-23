using Prism.Events;
using IndyPOS.Enums;

namespace IndyPOS.Events
{
	/// <summary>
	/// Event for notifying the active subpanel has changed on the Main form.
	/// </summary>
	public class ActiveSubpanelChangedEvent : PubSubEvent<Subpanel>
	{
	}
}
